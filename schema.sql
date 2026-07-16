
CREATE TABLE usuario (
    id serial PRIMARY KEY,
    nombre text,
    password text,
    dir_foto_perfil text
);

CREATE TABLE ingredientes (
    id serial PRIMARY KEY,
    nombre text NOT NULL,
    cantidad numeric NOT NULL DEFAULT 0,
    costo numeric,
    fecha_vencimiento date
);

CREATE TABLE productos (
    id serial PRIMARY KEY,
    nombre_producto text NOT NULL,
    costo numeric,
    precio numeric NOT NULL,
    cantidad_producto numeric,
    descuento_max numeric,
    beneficio numeric,
    descripcion text,
    categoria text
);

CREATE TABLE ingredientexproducto (
    id serial PRIMARY KEY,
    producto_id int REFERENCES productos(id),
    ingrediente_id int REFERENCES ingredientes(id)
);

CREATE TABLE cliente (
    id serial PRIMARY KEY,
    nombre text,
    telefono text,
    direccion text,
    credito numeric
);

CREATE TABLE metodo_de_pago (
    id serial PRIMARY KEY,
    nombre text
);

CREATE TABLE ventas (
    id serial PRIMARY KEY,
    cliente_id int REFERENCES cliente(id),
    metodo_pago_id int REFERENCES metodo_de_pago(id),
    fecha timestamp NOT NULL DEFAULT now(),
    total numeric NOT NULL,
    impuesto numeric DEFAULT 0,
    descuento numeric DEFAULT 0
);

CREATE TABLE productoxventa (
    id serial PRIMARY KEY,
    producto_id int REFERENCES productos(id),
    ventas_id int REFERENCES ventas(id)
);

-- Optional sample data so the app has something to show right away.
-- Delete or edit these before handing it to classmates.

INSERT INTO ingredientes (nombre, cantidad, costo) VALUES
    ('Harina', 50, 30),
    ('Azucar', 40, 25),
    ('Chocolate', 20, 90);

INSERT INTO productos (nombre_producto, costo, precio, cantidad_producto, categoria) VALUES
    ('Pan de agua', 15, 35, 100, 'Pan'),
    ('Brownie', 40, 90, 30, 'Postre');

-- link brownie -> chocolate + azucar
INSERT INTO ingredientexproducto (producto_id, ingrediente_id) VALUES
    (2, 3),
    (2, 2);
