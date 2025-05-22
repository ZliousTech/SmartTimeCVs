$(document).ready(function () {
	$('body').delegate('.js-render-modal', 'click', function () {
		var btn = $(this);
		var modal = $('#Modal');

		modal.find('#ModalTitleOperator').text(btn.data('operator'));
		modal.find('#ModalTitle').text(btn.data('title'));

		if (btn.data('update') !== undefined) {
			updatedRow = btn.parents('tr');
		}

		$.get({
			url: btn.data('url'),
			success: function (form) {
				modal.find('.modal-body').html(form);
				$.validator.unobtrusive.parse(modal);
			},
			error: function () {
				showErrorMessage();
			}
		})

		modal.modal('show');
	});
})