// jQuery unobtrusive validation defaults

$.validator.setDefaults({
    highlight: function (element) {
        $(element).removeClass("input-validation-error").removeClass("form-control-valid").addClass("form-control-invalid");

        // Check for specific classes like datepicker, datetimepicker, etc.
        $(element).each(function () {
            var classes = $(this).attr('class').split(" ");
            $.each(classes, function (index, className) {
                if (className.trim() === "datepicker" || className.trim() === "datetimepicker" || className.trim() === "timepicker" || className.trim() === "timeasnumber") {
                    $(element).prev('.input-group-addon').removeClass("form-control-icon-valid").addClass("form-control-icon-invalid");
                }
            });
        });

        // Add form-control-invalid class to the button under dropdown with select.
        $(element).parents('.dropdown').find('button').removeClass("form-control-valid").addClass('form-control-invalid');

        // Modify the error message display.
        $(element.form).find("[data-valmsg-for='" + element.name + "']").removeClass("invalid-feedback");
        var currentText = $(element.form).find("[data-valmsg-for='" + element.name + "']").text();
        $(element.form).find("[data-valmsg-for='" + element.name + "']").text(currentText.replace("The value '' is invalid.", "The " + element.name + " field is invalid."));
    },

    unhighlight: function (element) {
        $(element).removeClass("form-control-invalid").removeClass("input-validation-error").addClass("form-control-valid");
        $(element).each(function () {
            var classes = $(this).attr('class').split(" ");
            $.each(classes, function (index, className) {
                if (className.trim() === 'datepicker' || className.trim() === 'datetimepicker' || className.trim() === 'timepicker' || className.trim() === "timeasnumber") {
                    $(element).prev('.input-group-addon').removeClass("form-control-icon-invalid").addClass("form-control-icon-valid");
                }
            });
        });

        // Add form-control-invalid class to the button under dropdown with select.
        $(element).parents('.dropdown').find('button').removeClass("form-control-invalid").addClass('form-control-valid');
        
        $(element.form).find("[data-valmsg-for='" + element.name + "']").removeClass("invalid-feedback");
    },
});

