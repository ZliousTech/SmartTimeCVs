$(document).ready(function () {
    $('#save').on('click', function (e) {
        var purchasePrice = parseFloat($('#PurchasePrice').val());
        var sellingPrice = parseFloat($('#SellingPrice').val());

        if (purchasePrice > sellingPrice) {
            e.preventDefault();

            bootbox.confirm({
                title: 'Warning',
                message: 'The purchase price is greater than the selling price, are you sure you want to save this stock?',
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
                        $('#form').off('submit').submit();
                    }
                }
            });
        }
    });
});