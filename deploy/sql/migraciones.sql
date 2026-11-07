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

