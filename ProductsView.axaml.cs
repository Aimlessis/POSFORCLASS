using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;

namespace DolcePOSDummies
{
    public class Producto
    {
        public int Id { get; set; }
        public string NombreProducto { get; set; } = "";
        public decimal Costo { get; set; }
        public decimal Precio { get; set; }
        public decimal CantidadProducto { get; set; }
        public decimal DescuentoMax { get; set; }
        public decimal Beneficio { get; set; }
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";

        public override string ToString() => $"{NombreProducto} - {Precio:C}";
    }

    public partial class ProductsView : UserControl
    {
        private readonly ObservableCollection<Producto> _productos = new();

        public ProductsView()
        {
            InitializeComponent();
            Grid.ItemsSource = _productos;
            UpdateDatagrid();
        }


        private void UpdateDatagrid(string query = "SELECT id, nombre_producto, costo, precio, cantidad_producto, descuento_max, beneficio, descripcion, categoria FROM productos ORDER BY nombre_producto")
        {
            _productos.Clear();

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                _productos.Add(new Producto
                {
                    Id = reader.GetInt32(0),
                    NombreProducto = reader.GetString(1),
                    Costo = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2)),
                    Precio = Convert.ToDecimal(reader.GetValue(3)),
                    CantidadProducto = reader.IsDBNull(4) ? 0 : Convert.ToDecimal(reader.GetValue(4)),
                    DescuentoMax = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
                    Beneficio = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6)),
                    Descripcion = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Categoria = reader.IsDBNull(8) ? "" : reader.GetString(8)
                });
            }
        }

        private void InsertData()
        {
            decimal.TryParse(CostoBox.Text, out var costo);
            decimal.TryParse(PrecioBox.Text, out var precio);
            decimal.TryParse(CantidadBox.Text, out var cantidad);
            decimal.TryParse(DescuentoMaxBox.Text, out var descuentoMax);
            decimal.TryParse(BeneficioBox.Text, out var beneficio);


            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO productos (nombre_producto, costo, precio, cantidad_producto, descuento_max, beneficio, descripcion, categoria) " +
                "VALUES (@nombre, @costo, @precio, @cant, @desc_max, @benef, @descripcion, @categoria)", conn);
            cmd.Parameters.AddWithValue("nombre", NombreBox.Text ?? "");
            cmd.Parameters.AddWithValue("costo", costo);
            cmd.Parameters.AddWithValue("precio", precio);
            cmd.Parameters.AddWithValue("cant", cantidad);
            cmd.Parameters.AddWithValue("desc_max", descuentoMax);
            cmd.Parameters.AddWithValue("benef", beneficio);
            cmd.Parameters.AddWithValue("descripcion", DescripcionBox.Text ?? "");
            cmd.Parameters.AddWithValue("categoria", CategoriaBox.Text ?? "");
            cmd.ExecuteNonQuery();
        }

        private void UpdateData(int id)
        {
            decimal.TryParse(CostoBox.Text, out var costo);
            decimal.TryParse(PrecioBox.Text, out var precio);
            decimal.TryParse(CantidadBox.Text, out var cantidad);
            decimal.TryParse(DescuentoMaxBox.Text, out var descuentoMax);
            decimal.TryParse(BeneficioBox.Text, out var beneficio);


            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "UPDATE productos SET nombre_producto=@nombre, costo=@costo, precio=@precio, cantidad_producto=@cant, " +
                "descuento_max=@desc_max, beneficio=@benef, descripcion=@descripcion, categoria=@categoria WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("nombre", NombreBox.Text ?? "");
            cmd.Parameters.AddWithValue("costo", costo);
            cmd.Parameters.AddWithValue("precio", precio);
            cmd.Parameters.AddWithValue("cant", cantidad);
            cmd.Parameters.AddWithValue("desc_max", descuentoMax);
            cmd.Parameters.AddWithValue("benef", beneficio);
            cmd.Parameters.AddWithValue("descripcion", DescripcionBox.Text ?? "");
            cmd.Parameters.AddWithValue("categoria", CategoriaBox.Text ?? "");
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        private void DeleteData(int id)
        {
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM productos WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        private void Add_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreBox.Text))
                return;

            InsertData();
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void Update_Click(object? sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Producto selected)
                return;

            UpdateData(selected.Id);
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void Delete_Click(object? sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Producto selected)
                return;

            DeleteData(selected.Id);
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void BuscarProducto_TextChanged(object? sender, TextChangedEventArgs e)
        {
            string filtro = BuscarProducto.Text?.ToLower() ?? "";

            var resultado = _productos
                .Where(c =>
                    c.NombreProducto.ToLower().Contains(filtro) ||
                    c.Categoria.ToLower().Contains(filtro))
                .ToList();
                
            Grid.ItemsSource = resultado;
        }

        private void ClearForm_Click(object? sender, RoutedEventArgs e)
        {
            NombreBox.Text = "";
            CostoBox.Text = "";
            PrecioBox.Text = "";
            CantidadBox.Text = "";
            DescuentoMaxBox.Text = "";
            BeneficioBox.Text = "";
            DescripcionBox.Text = "";
            CategoriaBox.Text = "";
            Grid.SelectedItem = null;
        }

        private void Grid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Grid.SelectedItem is not Producto p)
                return;

            NombreBox.Text = p.NombreProducto;
            CostoBox.Text = p.Costo.ToString();
            PrecioBox.Text = p.Precio.ToString();
            CantidadBox.Text = p.CantidadProducto.ToString();
            DescuentoMaxBox.Text = p.DescuentoMax.ToString();
            BeneficioBox.Text = p.Beneficio.ToString();
            DescripcionBox.Text = p.Descripcion;
            CategoriaBox.Text = p.Categoria;
        }
    }
}
