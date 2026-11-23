using FluentAssertions;
using GoldmoneyBackend.Application.Common.Interfaces;

namespace GoldmoneyBackend.Application.Tests.Clientes;

public sealed class PaginacionClientesTests
{
    [Theory]
    [InlineData(1, 10, 1, 10)]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 500, 1, 100)]
    [InlineData(4, 25, 4, 25)]
    public void Normaliza_pagina_y_tamano(int pagina, int tamano, int paginaEsperada, int tamanoEsperado) =>
        PaginacionClientes.Normalizar(pagina, tamano).Should().Be((paginaEsperada, tamanoEsperado));
}
