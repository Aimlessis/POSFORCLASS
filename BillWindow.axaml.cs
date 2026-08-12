using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;

namespace DolcePOSDummies
{
    public partial class BillWindow : Window
    {
        private readonly ObservableCollection<VentaLinea> _billLines = new();

        public BillWindow(IEnumerable<SaleLine> items, decimal total)
        {
            InitializeComponent();
            _billLines.Clear();

            foreach (var item in items)
            {
                _billLines.Add(new VentaLinea
                {
                    Fecha = DateTime.Now,
                    Producto = item.Nombre,
                    Cantidad = item.Cantidad,
                    Precio = item.Precio
                });
            }

            BillGrid.ItemsSource = _billLines;
            BillTotal.Text = $"Total: {total:C}";
            Console.WriteLine($"Bill lines count: {_billLines.Count}");
        }

        private void CloseBill_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}