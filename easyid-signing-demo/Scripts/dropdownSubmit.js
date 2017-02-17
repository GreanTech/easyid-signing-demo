(function ($) {

    $('.dropdown-submit-input .dropdown-menu a').click(function (e) {
        e.preventDefault();
        $(this).closest('.dropdown-submit-input').find('input[name="selectedSignMethod"]').val($(this).data('value'));
        $(this).closest('form').submit();
    });
})(jQuery);