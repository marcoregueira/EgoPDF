$(document).ready(function () {

    $(document).on('click', '.sampleSource', function (evt) {
        evt.preventDefault();
        evt.stopPropagation();
        var sample = $(this).data('sample');
        var url = 'ViewSample?id=' + sample;
        $('<div />').addClass('modal').load(url, function () {
            $(this).modal({
                keyboard: true,
                backdrop: true
            });

            window.prettyPrint && prettyPrint();
        }).modal('show');
    });
});