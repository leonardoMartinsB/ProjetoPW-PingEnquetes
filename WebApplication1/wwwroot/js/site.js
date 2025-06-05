$(document).ready(function () {
    $('#logoutForm').submit(function (e) {
        e.preventDefault();

        $.post($(this).attr('action'), function (response) {
            window.location.href = '/Home/Index';
        }).fail(function () {
            alert('Erro ao tentar sair. Por favor, tente novamente.');
        });
    });
});