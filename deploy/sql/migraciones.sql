IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821031326_InitialCreate'
)
BEGIN
    CREATE TABLE [ClientesBackend] (
        [Id] uniqueidentifier NOT NULL,
        [Nombre] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Documento] nvarchar(20) NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaActualizacion] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ClientesBackend] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821031326_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientesBackend_Documento] ON [ClientesBackend] ([Documento]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821031326_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientesBackend_Email] ON [ClientesBackend] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821031326_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260821031326_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107175154_AgregarCorreoYRestablecimientoContrasena'
)
BEGIN
    CREATE TABLE [RestablecimientosContrasena] (
        [Id] uniqueidentifier NOT NULL,
        [NombreUsuario] varchar(15) NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [CreadoEnUtc] datetime2 NOT NULL,
        [ExpiraEnUtc] datetime2 NOT NULL,
        [UsadoEnUtc] datetime2 NULL,
        CONSTRAINT [PK_RestablecimientosContrasena] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107175154_AgregarCorreoYRestablecimientoContrasena'
)
BEGIN
    CREATE TABLE [UsuariosCorreo] (
        [NombreUsuario] varchar(15) NOT NULL,
        [Correo] nvarchar(256) NOT NULL,
        [ActualizadoEnUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UsuariosCorreo] PRIMARY KEY ([NombreUsuario])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107175154_AgregarCorreoYRestablecimientoContrasena'
)
BEGIN
    CREATE INDEX [IX_RestablecimientosContrasena_NombreUsuario_CreadoEnUtc] ON [RestablecimientosContrasena] ([NombreUsuario], [CreadoEnUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107175154_AgregarCorreoYRestablecimientoContrasena'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RestablecimientosContrasena_TokenHash] ON [RestablecimientosContrasena] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107175154_AgregarCorreoYRestablecimientoContrasena'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UsuariosCorreo_Correo] ON [UsuariosCorreo] ([Correo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107175154_AgregarCorreoYRestablecimientoContrasena'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107175154_AgregarCorreoYRestablecimientoContrasena', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108072216_AgregarCredencialesUsuario'
)
BEGIN
    CREATE TABLE [UsuariosCredencial] (
        [NombreUsuario] varchar(15) NOT NULL,
        [Hash] varchar(256) NOT NULL,
        [ActualizadoEnUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UsuariosCredencial] PRIMARY KEY ([NombreUsuario])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108072216_AgregarCredencialesUsuario'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108072216_AgregarCredencialesUsuario', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE TABLE [Cobros] (
        [Id] uniqueidentifier NOT NULL,
        [FechaUtc] datetime2 NOT NULL,
        [Usuario] nvarchar(50) NOT NULL,
        [IdCliente] varchar(14) NOT NULL,
        [TotalMetodos] decimal(18,2) NOT NULL,
        [TotalDocumentos] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Cobros] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE TABLE [CobrosDocumento] (
        [Id] int NOT NULL IDENTITY,
        [CobroId] uniqueidentifier NOT NULL,
        [Tipo] varchar(10) NOT NULL,
        [CodigoEmpresa] varchar(2) NOT NULL,
        [CodigoGrupo] int NOT NULL,
        [NumeroContrato] varchar(20) NOT NULL,
        [NumeroRecibo] int NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_CobrosDocumento] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CobrosDocumento_Cobros_CobroId] FOREIGN KEY ([CobroId]) REFERENCES [Cobros] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE TABLE [CobrosMetodoPago] (
        [Id] int NOT NULL IDENTITY,
        [CobroId] uniqueidentifier NOT NULL,
        [Metodo] varchar(20) NOT NULL,
        [Monto] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_CobrosMetodoPago] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CobrosMetodoPago_Cobros_CobroId] FOREIGN KEY ([CobroId]) REFERENCES [Cobros] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE INDEX [IX_Cobros_FechaUtc] ON [Cobros] ([FechaUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE INDEX [IX_Cobros_IdCliente] ON [Cobros] ([IdCliente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE INDEX [IX_CobrosDocumento_CobroId] ON [CobrosDocumento] ([CobroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CobrosDocumento_CodigoEmpresa_Tipo_NumeroRecibo] ON [CobrosDocumento] ([CodigoEmpresa], [Tipo], [NumeroRecibo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CobrosMetodoPago_CobroId_Metodo] ON [CobrosMetodoPago] ([CobroId], [Metodo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108073427_AgregarFormaPagoCobros'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108073427_AgregarFormaPagoCobros', N'8.0.8');
END;
GO

COMMIT;
GO

