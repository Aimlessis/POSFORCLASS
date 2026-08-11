using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using System.Linq;

namespace DolcePOSDummies
{
    public class Ingrediente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal Costo { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }

    public partial class IngredientsView : UserControl
    {
        private readonly ObservableCollection<Ingrediente> _ingredientes = new();

        public IngredientsView()
        {
            InitializeComponent();
            Grid.ItemsSource = _ingredientes;
            UpdateDatagrid();
        }


        private void UpdateDatagrid()
        {
            _ingredientes.Clear();

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, cantidad, costo, fecha_vencimiento FROM ingredientes ORDER BY nombre", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                _ingredientes.Add(new Ingrediente
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Cantidad = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader.GetValue(2)),
                    Costo = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3)),
                    FechaVencimiento = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                });
            }
        }

        private void InsertData()
        {
            decimal.TryParse(CantidadBox.Text, out var cantidad);
            decimal.TryParse(CostoBox.Text, out var costo);

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO ingredientes (nombre, cantidad, costo, fecha_vencimiento) VALUES (@nombre, @cantidad, @costo, @fecha)", conn);
            cmd.Parameters.AddWithValue("nombre", NombreBox.Text ?? "");
            cmd.Parameters.AddWithValue("cantidad", cantidad);
            cmd.Parameters.AddWithValue("costo", costo);
            cmd.Parameters.AddWithValue("fecha", (object?)FechaPicker.SelectedDate?.DateTime ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private void UpdateData(int id)
        {
            decimal.TryParse(CantidadBox.Text, out var cantidad);
            decimal.TryParse(CostoBox.Text, out var costo);

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "UPDATE ingredientes SET nombre=@nombre, cantidad=@cantidad, costo=@costo, fecha_vencimiento=@fecha WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("nombre", NombreBox.Text ?? "");
            cmd.Parameters.AddWithValue("cantidad", cantidad);
            cmd.Parameters.AddWithValue("costo", costo);
            cmd.Parameters.AddWithValue("fecha", (object?)FechaPicker.SelectedDate?.DateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        private void DeleteData(int id)
        {
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM ingredientes WHERE id=@id", conn);
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
            if (Grid.SelectedItem is not Ingrediente selected)
                return;

            UpdateData(selected.Id);
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void Delete_Click(object? sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Ingrediente selected)
                return;

            DeleteData(selected.Id);
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void ClearForm_Click(object? sender, RoutedEventArgs e)
        {
            NombreBox.Text = "";
            CantidadBox.Text = "";
            CostoBox.Text = "";
            FechaPicker.SelectedDate = null;
            Grid.SelectedItem = null;
        }

        private void BuscarIngrediente_TextChanged(object? sender, TextChangedEventArgs e)
        {
            string filtro = BuscarIngrediente.Text?.ToLower() ?? "";

            var resultados = _ingredientes
                .Where(c => 
                    c.Nombre.ToLower().Contains(filtro))
                .ToList();

            Grid.ItemsSource = resultados;
        }

        private void Grid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Grid.SelectedItem is not Ingrediente i)
                return;

            NombreBox.Text = i.Nombre;
            CantidadBox.Text = i.Cantidad.ToString();
            CostoBox.Text = i.Costo.ToString();
            FechaPicker.SelectedDate = i.FechaVencimiento;
        }
    }
}
