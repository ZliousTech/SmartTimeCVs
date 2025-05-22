document.addEventListener('DOMContentLoaded', () => {
    const fileInput = document.getElementById('ImageFile');
    const imagePreview = document.getElementById('imagePreview');
    const editButton = document.getElementById('editButton');
    const deleteButton = document.getElementById('deleteButton');

    fileInput.addEventListener('change', function (event) {
        const file = event.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                imagePreview.style.backgroundImage = `url(${e.target.result})`;
            };
            reader.readAsDataURL(file);
        }
    });

    deleteButton.addEventListener('click', function () {
        imagePreview.style.backgroundImage = `url(/images/CVs/ProfileImagePlaceholder.jpg)`;
        fileInput.value = '';
    });
});
$(document).ready(function () {
    const maxFileSize = 2 * 1024 * 1024; // 2 MB
    const allowedExtensions = ['.jpg', '.jpeg', '.png'];

    $('#form').on('submit', function (event) {
        const fileInput = $('#Image')[0];
        const file = fileInput.files[0];

        if (file) {
            const fileExtension = file.name.split('.').pop().toLowerCase();
            const fileSize = file.size;

            if (!allowedExtensions.includes(`.${fileExtension}`)) {
                $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
                $('.js-image-span-validation').text('Only .jpg, .jpeg and .png files are allowed!')
                event.preventDefault();
                return false;
            }
            else {
                $('.js-image-validation').removeClass('form-control-invalid').addClass('form-control-valid');
                $('.js-image-span-validation').empty();
            }

            if (fileSize > maxFileSize) {
                $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
                $('.js-image-span-validation').text('File cannot be more than 2 MB!')
                event.preventDefault();
                return false;
            }
            else {
                $('.js-image-validation').removeClass('form-control-invalid').addClass('form-control-valid');
                $('.js-image-span-validation').empty();
            }
        }
    });
});