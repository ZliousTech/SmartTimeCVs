$(document).ready(function () {
    // Destroy the select picker for a specific element
    $('.js-select2').selectpicker('destroy');
    $('.js-select2').select2({
        theme: 'bootstrap4',
        dropdownPosition: 'below',
        width: '100%'
    });

    $('.js-select2').on('select2:select', function () {
        $('form').validate().element('#' + $(this).attr('id'))
    });
});