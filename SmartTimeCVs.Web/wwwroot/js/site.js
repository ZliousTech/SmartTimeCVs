// Begin DataTables
var table;
var exportedCols = [];
var excludedsearchCols = [];
var updatedRow;
var saveMessage = "Saved Successfully"
var updateMessage = "Updated Successfully"
var deleteMessage = "Deleted Successfully"
var retriveMessage = "Retrived Successfully"
var messageToShow = saveMessage;

function tableExportedColumns() {
	var headers = $('th');
	$.each(headers, function (i) {
		if (!$(this).hasClass('js-no-export'))
			exportedCols.push(i);
	});

	return exportedCols;
}

function tableExcludedSearchColumns() {
	var headers = $('th');
	$.each(headers, function (i) {
		if ($(this).hasClass('js-exclude-search'))
			excludedsearchCols.push(i);
	});

	return excludedsearchCols;
}

function initDatatable(exportedCols, excludedsearchCols) {
	const documentTitle = $('.js-datatables').data('document-title');
	table = $('.js-datatables').DataTable({
		lengthMenu: [5, 10, 25, 50, 100],
		dom: 'lBfrtip',
		buttons: [
			{
				extend: 'copy',
				className: 'btn btn-primary',
				title: documentTitle,
				exportOptions: {
					columns: exportedCols
				}
			},
			{
				extend: 'csv',
				className: 'btn btn-primary',
				title: documentTitle,
				exportOptions: {
					columns: exportedCols
				}
			},
			{
				extend: 'print',
				className: 'btn btn-primary',
				title: documentTitle,
				exportOptions: {
					columns: exportedCols
				}
			}
		],
		order: [[0, 'desc']],
		columnDefs: [{ searchable: false, targets: excludedsearchCols }]
	});
}

// Handle data update in real time.
function drawDatatable() {
	if (typeof table !== 'undefined') {
		table.draw();
	}
}

function getColumnIndexByClass(columnClass) {
	var headers = table.columns().header();
	var index = -1;
	$(headers).each(function (i, header) {
		if ($(header).hasClass(columnClass)) {
			index = i;
		}
	});
	return index;
}

function updateDatatableData(row, currentStatus, btn, lastUpdatedOn, currentPage) {
	var rowData = table.row(row).data();
	var statusColumnIndex = getColumnIndexByClass('js-status-header');
	var newStatus = currentStatus === 'Deleted' ? 'Available' : 'Deleted';
	var newStatusHtml = newStatus === 'Deleted' ?
		'<span class="badge badge-danger js-status">Deleted</span>' :
		'<span class="badge badge-success js-status">Available</span>';
	rowData[statusColumnIndex] = newStatusHtml

	var controllerName = btn.data('name').split(' ').join('');
	
	var modelName = btn.data('name');

	// Update DataTable row
	table.row(row).data(rowData).draw();

	row.find('.js-updated-on').html(lastUpdatedOn);

	messageToShow = btn.text() === 'Delete' ? deleteMessage : retriveMessage;
	showSuccessMessageWithAnimation(row, messageToShow);

	// Update Actions dropdown list based on new status.
	var dropdownMenu = row.find('.js-table-actions');
	dropdownMenu.empty();

	if (newStatus === 'Deleted') {
		dropdownMenu.html('<li class="js-delete-retrieve" data-id=' + btn.data('id') + ' data-url="/' + controllerName + '/ToggleStatus/' + btn.data('id') + '" data-name="' + modelName +'"><a class="js-toggle" href="javascript:;">Retrieve</a></li>');
	} else {
		if (dropdownMenu.data('for-modal') === undefined) {
			dropdownMenu.html(
				'<li><a href="Customer/Edit/' + btn.data('id') + '?isFromJobApplicationView=true">Edit</a></li>' +
				'<li class="js-delete-retrieve" data-id=' + btn.data('id') + ' data-url="/' + controllerName + '/ToggleStatus/' + btn.data('id') + '" data-name="' + modelName + '"><a class="js-toggle" href="javascript:;">Delete</a></li>'
			);
		}
		else {
			dropdownMenu.html(
				'<li><a href="javascript:;" class="js-render-modal" data-operator="Edit" data-title="' + btn.data('name') + '" data-url="Customer/Edit/' + btn.data('id') + '?isFromJobApplicationView=true" data-update="true">Edit</a></li>' +
				'<li class="js-delete-retrieve" data-id=' + btn.data('id') + ' data-url="/' + controllerName + '/ToggleStatus/' + btn.data('id') + '" data-name="' + modelName + '"><a class="js-toggle" href="javascript:;">Delete</a></li>'
			);
		}
		
	}

	table.page(currentPage).draw('page');
}

// End DataTables

// Begin sweetAlerts
function showSuccessMessage(message = "Saved Successfully") {
	Swal.fire({
		icon: "success",
		title: "Success",
		text: message
	});
}

function showErrorMessage(message = "Something went wrong") {
	Swal.fire({
		icon: "error",
		title: "Oops...",
		text: message
	});
}

function showSuccessMessageWithAnimation(row, message = "Saved Successfully") {
	row.removeClass('animate__animated animate__flash');
	Swal.fire({
		icon: "success",
		title: "Success",
		text: message
	}).then((result) => {
		if (result.isConfirmed) {
			row.addClass('animate__animated animate__flash');
			setTimeout(() => {
				row.removeClass('animate__animated animate__flash');
			}, 2000);
		}
	});
}
// End sweetAlerts

// Begin javascript

function toggleStatusRequest() {
	$('body').delegate('.js-delete-retrieve', 'click', function () {
		var btn = $(this);
		var moduleName = btn.data('name');
		var row = btn.closest('tr');
		var currentStatus = row.find('.js-status').text();
		var currentToggleText = row.find('.js-toggle').text();
		var currentPage = table.page.info().page;

		bootbox.confirm({
			title: currentToggleText + ' ' + moduleName,
			message: 'Are you sure that you need to ' + currentToggleText.toLowerCase() + ' this ' + moduleName.toLowerCase() + '?',
			buttons: {
				cancel: {
					label: 'No'
				},
				confirm: {
					label: 'Yes'
				}
			},
			callback: function (result) {
				if (result) {
					$.post({
						url: btn.data('url'),
						data: {
							'__RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
						},
						success: function (lastUpdatedOn) {
							updateDatatableData(row, currentStatus, btn, lastUpdatedOn, currentPage);
						},
						error: function () {
							showErrorMessage();
						}
					})
				}
			}
		});
	})
}

function onRequestBegin() {
	$('.js-correct-icon').removeClass('col-5').addClass('col-10').empty().html('<b>Please wait...</b>');
	$('.js-button-text').removeClass('col-7').addClass('col-2 spinner-border').empty();
	$('body :submit').attr('disabled', 'disabled');
}

function onRequestSuccess() {
	window.location.href = $('body :submit').data('index-url');
}

function onModalRequestSuccess(row) {
	$('#Modal').modal('hide');

	if (updatedRow !== undefined) {
		table.row(updatedRow).remove().draw();
		updatedRow = undefined;
		messageToShow = updateMessage;
	}

	var newRow = $(row);
	table.row.add(newRow).draw();

	showSuccessMessageWithAnimation(newRow, messageToShow)
	messageToShow = saveMessage;
}

function onRequestFailure() {
	showErrorMessage();
}

function onRequestComplete() {
	$('.js-correct-icon').removeClass('col-10').addClass('col-5').empty().html('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-check2" viewBox="0 0 16 16">'
		+ '<path d = "M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0" />'
		+ '</svg>');
	$('.js-button-text').removeClass('col-2  spinner-border').addClass('col-7').empty().html('<b>Save</b>');
	$('body :submit').removeAttr('disabled');
}

$(document).ready(function () {

	var message = $('#Message').text();
	if (message !== '') {
		showSuccessMessage(message);
	}
})
// End javascript