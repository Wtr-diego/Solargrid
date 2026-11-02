-- SOLARGRID - Creacion completa de la base de datos para SQL Server / SSMS.
-- NUEVA INSTALACION. No borra bases de datos, tablas ni registros existentes.
-- IMPORTANTE: si ya tienes datos en LocalDB y deseas conservarlos,
-- NO ejecutes esto para intentar trasladarlos. Usa RESPALDO + RESTAURACION
-- y luego actualiza la conexion (ver GUIA_SQL_SERVER.md).
-- Ejecucion: en SSMS conecta a TU instancia, abre este archivo, pulsa F5.

USE [master];
GO
IF DB_ID(N'solagrid') IS NULL
BEGIN
    CREATE DATABASE [solagrid];
    PRINT N'Base solagrid creada.';
END
ELSE
    PRINT N'La base solagrid ya existia. No se ha borrado nada.';
GO
USE [solagrid];
GO

IF OBJECT_ID(N'dbo.Comunidades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Comunidades (
        IdComunidad INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comunidades PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Ubicacion NVARCHAR(150) NOT NULL,
        Estado NVARCHAR(20) NOT NULL,
        CONSTRAINT CK_Comunidades_Estado CHECK (Estado IN (N'Activa', N'Inactiva'))
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Comunidades') AND name=N'IX_Comunidades_Nombre')
    CREATE UNIQUE INDEX IX_Comunidades_Nombre ON dbo.Comunidades(Nombre);
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        IdUsuario INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        IdComunidad INT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        Correo NVARCHAR(160) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Rol NVARCHAR(30) NOT NULL,
        Activo BIT NOT NULL,
        CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (N'Administrador',N'Tecnico',N'Comunidad')),
        CONSTRAINT FK_Usuarios_Comunidades_IdComunidad FOREIGN KEY (IdComunidad)
          REFERENCES dbo.Comunidades(IdComunidad)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'IX_Usuarios_Correo')
    CREATE UNIQUE INDEX IX_Usuarios_Correo ON dbo.Usuarios(Correo);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Usuarios') AND name=N'IX_Usuarios_IdComunidad')
    CREATE INDEX IX_Usuarios_IdComunidad ON dbo.Usuarios(IdComunidad);
GO

IF OBJECT_ID(N'dbo.SistemasSolares', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SistemasSolares (
        IdSistema INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SistemasSolares PRIMARY KEY,
        IdComunidad INT NOT NULL,
        Nombre NVARCHAR(120) NOT NULL,
        FechaInstalacion DATETIME2 NOT NULL,
        CapacidadKw DECIMAL(10,2) NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        CONSTRAINT CK_Sistemas_Capacidad CHECK (CapacidadKw > 0),
        CONSTRAINT FK_SistemasSolares_Comunidades_IdComunidad FOREIGN KEY (IdComunidad)
          REFERENCES dbo.Comunidades(IdComunidad)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SistemasSolares') AND name=N'IX_SistemasSolares_IdComunidad')
    CREATE INDEX IX_SistemasSolares_IdComunidad ON dbo.SistemasSolares(IdComunidad);
GO

IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipos (
        IdEquipo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipos PRIMARY KEY,
        IdSistema INT NOT NULL,
        Codigo NVARCHAR(40) NOT NULL,
        Tipo NVARCHAR(40) NOT NULL,
        Marca NVARCHAR(80) NOT NULL,
        Modelo NVARCHAR(80) NOT NULL,
        Estado NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_Equipos_SistemasSolares_IdSistema FOREIGN KEY (IdSistema)
          REFERENCES dbo.SistemasSolares(IdSistema)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Equipos') AND name=N'IX_Equipos_Codigo')
    CREATE UNIQUE INDEX IX_Equipos_Codigo ON dbo.Equipos(Codigo);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Equipos') AND name=N'IX_Equipos_IdSistema')
    CREATE INDEX IX_Equipos_IdSistema ON dbo.Equipos(IdSistema);
GO

IF OBJECT_ID(N'dbo.UbicacionesPanel', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UbicacionesPanel (
        IdEquipo INT NOT NULL CONSTRAINT PK_UbicacionesPanel PRIMARY KEY,
        Latitud FLOAT NOT NULL,
        Longitud FLOAT NOT NULL,
        FechaActualizacion DATETIME2 NOT NULL,
        CONSTRAINT CK_Ubicacion_Latitud CHECK (Latitud BETWEEN -90 AND 90),
        CONSTRAINT CK_Ubicacion_Longitud CHECK (Longitud BETWEEN -180 AND 180),
        CONSTRAINT FK_UbicacionesPanel_Equipos_IdEquipo FOREIGN KEY (IdEquipo)
          REFERENCES dbo.Equipos(IdEquipo) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.LecturasEnergia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LecturasEnergia (
        IdLectura INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LecturasEnergia PRIMARY KEY,
        IdSistema INT NOT NULL,
        IdUsuario INT NOT NULL,
        FechaHora DATETIME2 NOT NULL,
        GeneracionKwh DECIMAL(14,2) NOT NULL,
        ConsumoKwh DECIMAL(14,2) NOT NULL,
        CONSTRAINT CK_Lecturas_Generacion CHECK (GeneracionKwh >= 0),
        CONSTRAINT CK_Lecturas_Consumo CHECK (ConsumoKwh >= 0),
        CONSTRAINT FK_LecturasEnergia_SistemasSolares_IdSistema FOREIGN KEY (IdSistema)
          REFERENCES dbo.SistemasSolares(IdSistema),
        CONSTRAINT FK_LecturasEnergia_Usuarios_IdUsuario FOREIGN KEY (IdUsuario)
          REFERENCES dbo.Usuarios(IdUsuario)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LecturasEnergia') AND name=N'IX_LecturasEnergia_IdSistema')
    CREATE INDEX IX_LecturasEnergia_IdSistema ON dbo.LecturasEnergia(IdSistema);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LecturasEnergia') AND name=N'IX_LecturasEnergia_IdUsuario')
    CREATE INDEX IX_LecturasEnergia_IdUsuario ON dbo.LecturasEnergia(IdUsuario);
GO

IF OBJECT_ID(N'dbo.Incidencias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Incidencias (
        IdIncidencia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Incidencias PRIMARY KEY,
        IdSistema INT NOT NULL,
        IdEquipo INT NULL,
        IdReporta INT NOT NULL,
        Descripcion NVARCHAR(1000) NOT NULL,
        Prioridad NVARCHAR(20) NOT NULL,
        Estado NVARCHAR(20) NOT NULL,
        FechaCreacion DATETIME2 NOT NULL,
        Solucion NVARCHAR(1000) NULL,
        CONSTRAINT CK_Incidencias_Estado CHECK (Estado IN (N'Abierta',N'En proceso',N'Resuelta',N'Cerrada')),
        CONSTRAINT FK_Incidencias_SistemasSolares_IdSistema FOREIGN KEY (IdSistema)
          REFERENCES dbo.SistemasSolares(IdSistema),
        CONSTRAINT FK_Incidencias_Equipos_IdEquipo FOREIGN KEY (IdEquipo)
          REFERENCES dbo.Equipos(IdEquipo),
        CONSTRAINT FK_Incidencias_Usuarios_IdReporta FOREIGN KEY (IdReporta)
          REFERENCES dbo.Usuarios(IdUsuario)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Incidencias') AND name=N'IX_Incidencias_IdSistema')
    CREATE INDEX IX_Incidencias_IdSistema ON dbo.Incidencias(IdSistema);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Incidencias') AND name=N'IX_Incidencias_IdEquipo')
    CREATE INDEX IX_Incidencias_IdEquipo ON dbo.Incidencias(IdEquipo);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Incidencias') AND name=N'IX_Incidencias_IdReporta')
    CREATE INDEX IX_Incidencias_IdReporta ON dbo.Incidencias(IdReporta);
GO

IF OBJECT_ID(N'dbo.Reportes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reportes (
        IdReporte INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reportes PRIMARY KEY,
        IdUsuario INT NOT NULL,
        IdSistema INT NOT NULL,
        Tipo NVARCHAR(50) NOT NULL,
        PeriodoInicio DATETIME2 NOT NULL,
        PeriodoFin DATETIME2 NOT NULL,
        CONSTRAINT CK_Reportes_Periodo CHECK (PeriodoInicio <= PeriodoFin),
        CONSTRAINT FK_Reportes_Usuarios_IdUsuario FOREIGN KEY (IdUsuario)
          REFERENCES dbo.Usuarios(IdUsuario),
        CONSTRAINT FK_Reportes_SistemasSolares_IdSistema FOREIGN KEY (IdSistema)
          REFERENCES dbo.SistemasSolares(IdSistema)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Reportes') AND name=N'IX_Reportes_IdUsuario')
    CREATE INDEX IX_Reportes_IdUsuario ON dbo.Reportes(IdUsuario);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Reportes') AND name=N'IX_Reportes_IdSistema')
    CREATE INDEX IX_Reportes_IdSistema ON dbo.Reportes(IdSistema);
GO

IF OBJECT_ID(N'dbo.Historial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Historial (
        IdHistorial INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Historial PRIMARY KEY,
        IdUsuario INT NOT NULL,
        Tabla NVARCHAR(60) NOT NULL,
        IdRegistro INT NOT NULL,
        Accion NVARCHAR(20) NOT NULL,
        FechaHora DATETIME2 NOT NULL,
        Detalle NVARCHAR(500) NOT NULL,
        CONSTRAINT FK_Historial_Usuarios_IdUsuario FOREIGN KEY (IdUsuario)
          REFERENCES dbo.Usuarios(IdUsuario)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Historial') AND name=N'IX_Historial_IdUsuario')
    CREATE INDEX IX_Historial_IdUsuario ON dbo.Historial(IdUsuario);
GO

PRINT N'SolarGrid: revisa el resultado de SQL/02_Verificar_Base.sql.';
PRINT N'Los usuarios de demostracion se crean de forma segura al iniciar el sitio en modo Development (F5) si no hay usuarios.';
