using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;

namespace DolcePOSDummies
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Direccion { get; set; } = "";
        public decimal Credito { get; set; }
    }

    public partial class ClientsView : UserControl
    {
        private readonly ObservableCollection<Cliente> _clientes = new();

        public ClientsView()
        {
            InitializeComponent();
            Grid.ItemsSource = _clientes;
            UpdateDatagrid();
        }

        private void UpdateDatagrid()
        {
            _clientes.Clear();

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, nombre, telefono, direccion, credito FROM cliente ORDER BY nombre", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                _clientes.Add(new Cliente
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Telefono = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Direccion = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Credito = reader.IsDBNull(4) ? 0 : Convert.ToDecimal(reader.GetValue(4))
                });
            }
        }

        private void InsertData()
        {
            decimal.TryParse(CreditoBox.Text, out var credito);

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO cliente (nombre, telefono, direccion, credito) VALUES (@nombre, @telefono, @direccion, @credito)", conn);
            cmd.Parameters.AddWithValue("nombre", NombreBox.Text ?? "");
            cmd.Parameters.AddWithValue("telefono", TelefonoBox.Text ?? "");
            cmd.Parameters.AddWithValue("direccion", DireccionBox.Text ?? "");
            cmd.Parameters.AddWithValue("credito", credito);
            cmd.ExecuteNonQuery();
        }

        private void UpdateData(int id)
        {
            decimal.TryParse(CreditoBox.Text, out var credito);

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "UPDATE cliente SET nombre=@nombre, telefono=@telefono, direccion=@direccion, credito=@credito WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("nombre", NombreBox.Text ?? "");
            cmd.Parameters.AddWithValue("telefono", TelefonoBox.Text ?? "");
            cmd.Parameters.AddWithValue("direccion", DireccionBox.Text ?? "");
            cmd.Parameters.AddWithValue("credito", credito);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        private void DeleteData(int id)
        {
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM cliente WHERE id=@id", conn);
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
            if (Grid.SelectedItem is not Cliente selected)
                return;

            UpdateData(selected.Id);
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void Delete_Click(object? sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is not Cliente selected)
                return;

            DeleteData(selected.Id);
            UpdateDatagrid();
            ClearForm_Click(null, null!);
        }

        private void ClearForm_Click(object? sender, RoutedEventArgs e)
        {
            NombreBox.Text = "";
            TelefonoBox.Text = "";
            DireccionBox.Text = "";
            CreditoBox.Text = "";
            Grid.SelectedItem = null;
        }

        private void Grid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Grid.SelectedItem is not Cliente c)
                return;

            NombreBox.Text = c.Nombre;
            TelefonoBox.Text = c.Telefono;
            DireccionBox.Text = c.Direccion;
            CreditoBox.Text = c.Credito.ToString();
        }
    }
}
