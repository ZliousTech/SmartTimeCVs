document.addEventListener('DOMContentLoaded', () => {
    const fileInput = document.getElementById('ImageFile');
    const imagePreview = document.getElementById('imagePreview');
    const editButton = document.getElementById('editButton');
    const deleteButton = document.getElementById('deleteButton');

    fileInput.addEventListener('change', function (event) {

        if (!validateImageInput(event)) {
            return;
        }

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

//function validateImageInput(event = null) {
//    const maxFileSize = 5 * 1024 * 1024; // 5 MB
//    const allowedExtensions = ['.jpg', '.jpeg', '.png'];

//    const fileInput = $('#ImageFile')[0];
//    const file = fileInput?.files?.[0];

//    if (file) {
//        const fileExtension = file.name.split('.').pop().toLowerCase();
//        const fileSize = file.size;

//        if (!allowedExtensions.includes(`.${fileExtension}`)) {
//            $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
//            $('.js-image-span-validation').text('Only .jpg, .jpeg and .png files are allowed!');
//            if (event) event.preventDefault();
//            return false;
//        }

//        if (fileSize > maxFileSize) {
//            $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
//            $('.js-image-span-validation').text('File cannot be more than 5 MB!');
//            if (event) event.preventDefault();
//            return false;
//        }

//        // Valid image
//        $('.js-image-validation').removeClass('form-control-invalid').addClass('form-control-valid');
//        $('.js-image-span-validation').empty();
//        return true;
//    } else {
//        $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
//        $('.js-image-span-validation').text('The Profile Image field is required.');
//        if (event) event.preventDefault();
//        return false;
//    }
//}

function validateImageInput(event = null) {
    const maxFileSize = 5 * 1024 * 1024; // 5 MB
    const allowedExtensions = ['.jpg', '.jpeg', '.png'];

    const fileInput = $('#ImageFile')[0];
    const file = fileInput?.files?.[0];

    const imagePreview = document.getElementById('imagePreview');
    const previewBackground = imagePreview?.style?.backgroundImage;

    const isPlaceholder = previewBackground.includes("ProfileImagePlaceholder.jpg");

    if (file) {
        const fileExtension = file.name.split('.').pop().toLowerCase();
        const fileSize = file.size;

        if (!allowedExtensions.includes(`.${fileExtension}`)) {
            $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
            $('.js-image-span-validation').text('Only .jpg, .jpeg and .png files are allowed!');
            if (event) event.preventDefault();
            return false;
        }

        if (fileSize > maxFileSize) {
            $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
            $('.js-image-span-validation').text('File cannot be more than 5 MB!');
            if (event) event.preventDefault();
            return false;
        }

        // Valid image
        $('.js-image-validation').removeClass('form-control-invalid').addClass('form-control-valid');
        $('.js-image-span-validation').empty();
        return true;
    } else {
        // If there's already a non-placeholder image, consider it valid (Edit view)
        if (!isPlaceholder) {
            $('.js-image-validation').removeClass('form-control-invalid').addClass('form-control-valid');
            $('.js-image-span-validation').empty();
            return true;
        }

        // Otherwise, it's invalid (Add view with no image)
        $('.js-image-validation').removeClass('form-control-valid').addClass('form-control-invalid');
        $('.js-image-span-validation').text('The Profile Image field is required.');
        if (event) event.preventDefault();
        return false;
    }
}



$(document).ready(function () {

    $('#ImageFile').on('change', function () {

        if (validateImageInput()) {
            $('.js-image-validation').removeClass('form-control-invalid').addClass('form-control-valid');
            $('.js-image-span-validation').empty();
        }
    });

    $('#form').on('submit', function (event) {
        validateImageInput(event);
    });
});