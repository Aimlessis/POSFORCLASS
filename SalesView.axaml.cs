using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;

namespace DolcePOSDummies
{
    public class SaleLine
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal => Precio * Cantidad;
    }

    public partial class SalesView : UserControl
    {
        private readonly ObservableCollection<SaleLine> _cart = new();

        public SalesView()
        {
            InitializeComponent();
            SalesGrid.ItemsSource = _cart;
            ProductCombo.DropDownOpened += (s, e) => UpdateDatagrid();
            UpdateDatagrid();
        }

        private void UpdateDatagrid()
        {
            try
            {
                var productos = new List<Producto>();

                using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    "SELECT id, nombre_producto, precio FROM productos ORDER BY nombre_producto", conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    productos.Add(new Producto
                    {
                        Id = reader.GetInt32(0),
                        NombreProducto = reader.GetString(1),
                        Precio = reader.GetDecimal(2)
                    });
                }

                ProductCombo.ItemsSource = productos;
            }
            catch (Exception ex)
            {
                TotalText.Text = "DB error - check connection string";
                Console.WriteLine(ex);
            }
        }

        private void InsertData()
{
    var total = _cart.Sum(l => l.Subtotal);

    using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
    conn.Open();
    using var tx = conn.BeginTransaction();

    try
    {
        int ventaId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO ventas (fecha, total, impuesto, descuento) VALUES (@fecha, @total, 0, 0) RETURNING id",
            conn, tx))
        {
            cmd.Parameters.AddWithValue("fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("total", total);
            ventaId = (int)cmd.ExecuteScalar()!;
        }

        foreach (var line in _cart)
        {
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO productoxventa (producto_id, ventas_id, cantidad) VALUES (@p, @v, @c)", conn, tx))
            {
                cmd.Parameters.AddWithValue("p", line.ProductoId);
                cmd.Parameters.AddWithValue("v", ventaId);
                cmd.Parameters.AddWithValue("c", line.Cantidad);
                cmd.ExecuteNonQuery();
            }

            using var linkCmd = new NpgsqlCommand(
                "SELECT ingrediente_id FROM ingredientexproducto WHERE producto_id = @p", conn, tx);
            linkCmd.Parameters.AddWithValue("p", line.ProductoId);

            var ingredienteIds = new List<int>();
            using (var linkReader = linkCmd.ExecuteReader())
            {
                while (linkReader.Read())
                    ingredienteIds.Add(linkReader.GetInt32(0));
            }

            foreach (var ingredienteId in ingredienteIds)
            {
                using var deductCmd = new NpgsqlCommand(
                    "UPDATE ingredientes SET cantidad = cantidad - @cant WHERE id = @id", conn, tx);
                deductCmd.Parameters.AddWithValue("cant", (decimal)line.Cantidad);
                deductCmd.Parameters.AddWithValue("id", ingredienteId);
                deductCmd.ExecuteNonQuery();
            }
        }

        tx.Commit();
    }
    catch (Exception)
    {
        tx.Rollback();
        throw;
    }
}

        private void AddItem_Click(object? sender, RoutedEventArgs e)
        {
            if (ProductCombo.SelectedItem is not Producto producto)
                return;

            if (!int.TryParse(QuantityBox.Text, out var qty) || qty <= 0)
                qty = 1;

            var existing = _cart.FirstOrDefault(l => l.ProductoId == producto.Id);
            if (existing != null)
            {
                existing.Cantidad += qty;
                SalesGrid.ItemsSource = null;
                SalesGrid.ItemsSource = _cart;
            }
            else
            {
                _cart.Add(new SaleLine
                {
                    ProductoId = producto.Id,
                    Nombre = producto.NombreProducto,
                    Precio = producto.Precio,
                    Cantidad = qty
                });
            }

            QuantityBox.Text = "1";
            UpdateTotal();
        }

        private void RemoveItem_Click(object? sender, RoutedEventArgs e)
        {
            if (SalesGrid.SelectedItem is SaleLine line)
            {
                _cart.Remove(line);
                UpdateTotal();
            }
        }

        private void ClearItems_Click(object? sender, RoutedEventArgs e)
        {
            _cart.Clear();
            UpdateTotal();
        }

private void Checkout_Click(object? sender, RoutedEventArgs e)
{
    Console.WriteLine($"[1] Checkout start, cart count: {_cart.Count}");

    if (_cart.Count == 0)
        return;

    try
    {
        InsertData();
        Console.WriteLine($"[2] After InsertData, cart count: {_cart.Count}");
        ShowBill();
        _cart.Clear();
        TotalText.Text = "Sale saved!";
    }
    catch (Exception ex)
    {
        TotalText.Text = "Checkout failed - see console";
        Console.WriteLine(ex);
    }
}

private void ShowBill()
{
    Console.WriteLine($"[3] ShowBill start, cart count: {_cart.Count}");
    var total = _cart.Sum(l => l.Subtotal);
    var billWindow = new BillWindow(_cart, total);
    billWindow.Show();
}

        private void ShowReports_Click(object? sender, RoutedEventArgs e)
        {
            var reportWindow = new ReportWindow();
            reportWindow.Show();
        }

        private void UpdateTotal()
        {
            var total = _cart.Sum(l => l.Subtotal);
            TotalText.Text = $"Total: {total:C}";
        }
    }
}