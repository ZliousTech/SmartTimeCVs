using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartTimeCVs.Web.Data;
using SmartTimeCVs.Web.Core.Models;
using System.IO;

namespace SmartTimeCVs.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var connectionString = "Server=(localdb)\\mssqllocaldb;Database=aspnet-SmartTimeCVs.Web-46067b84-4061-49f8-842c-292199b4ae7b;Trusted_Connection=True;MultipleActiveResultSets=true";
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(connectionString));
                }).Build();

            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var employeeName = "Ra'ad Fawzi Mohammad AL Sari AL Sahori";
                var nationalId = "2000628930";

                var contract = context.Contracts
                    .Include(c => c.JobApplication)
                    .FirstOrDefault(c => c.EmployeeName == employeeName && c.EmployeeNationalId == nationalId);

                if (contract != null)
                {
                    Console.WriteLine($"Found Contract ID: {contract.Id}");
                    
                    // Delete attachments
                    var attachments = context.ContractAttachments.Where(a => a.ContractId == contract.Id).ToList();
                    foreach (var attr in attachments)
                    {
                        if (!string.IsNullOrEmpty(attr.FileUrl))
                        {
                            var path = Path.Combine("wwwroot/images/attachments", attr.FileUrl);
                            if (File.Exists(path)) File.Delete(path);
                        }
                        context.ContractAttachments.Remove(attr);
                    }

                    // Delete main files
                    if (!string.IsNullOrEmpty(contract.SignedContractUrl))
                    {
                        var path = Path.Combine("wwwroot/images/attachments", contract.SignedContractUrl);
                        if (File.Exists(path)) File.Delete(path);
                    }
                    if (!string.IsNullOrEmpty(contract.NationalIdUrl))
                    {
                        var path = Path.Combine("wwwroot/images/attachments", contract.NationalIdUrl);
                        if (File.Exists(path)) File.Delete(path);
                    }

                    context.Contracts.Remove(contract);
                    context.SaveChanges();
                    Console.WriteLine("Successfully deleted the contract and its associated files.");
                }
                else
                {
                    Console.WriteLine("Contract not found.");
                }
            }
        }
    }
}
