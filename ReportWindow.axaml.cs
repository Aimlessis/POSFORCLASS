using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DolcePOSDummies
{
    public class VentaLinea
    {
        public DateTime Fecha { get; set; }
        public string Producto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Precio * Cantidad;
    }

    public partial class ReportWindow : Window
    {
        public ObservableCollection<VentaLinea> ReportLines { get; } = new();

        public string SelectedPeriod { get; set; } = "TODOS";
        public DateTime SelectedDate { get; set; } = DateTime.Now;

        public ReportWindow()
        {
            InitializeComponent();
            Periodos = new List<string>
            {
                "Todos los tiempos",
                "Mes",
                "Semana",
                "Día",
                "Año"
            };
            DataContext = this;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public List<string> Periodos { get; set; }

        private DateTime GetFechaInicio()
        {
            var date = SelectedDate;
            switch (SelectedPeriod)
            {
                case "Mes":
                    return new DateTime(date.Year, date.Month, 1);
                case "Semana":
                    int daysToMonday = (7 - (int)date.DayOfWeek) % 7;
                    return date.AddDays(-daysToMonday);
                case "Día":
                    return date.AddDays(-1);
                case "Año":
                    return new DateTime(date.Year, 1, 1);
                default:
                    return DateTime.MinValue;
            }
        }

        private DateTime GetFechaFin()
        {
            var date = SelectedDate;
            switch (SelectedPeriod)
            {
                case "Mes":
                    return new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);
                case "Semana":
                    int daysToSunday = (int)date.DayOfWeek;
                    return date.AddDays(6 - daysToSunday);
                case "Día":
                    return date;
                case "Año":
                    return new DateTime(date.Year, 12, 31);
                default:
                    return DateTime.MaxValue;
            }
        }

        private void Actualizar()
{
    ReportLines.Clear();

    DateTime inicio = GetFechaInicio();
    DateTime fin = GetFechaFin();

    using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
    conn.Open();

   string query = @"
    SELECT v.id, v.fecha, p.nombre_producto, pox.cantidad, p.precio
    FROM ventas v
    JOIN productoxventa pox ON v.id = pox.ventas_id
    JOIN productos p ON pox.producto_id = p.id
    WHERE v.total > 0::money
    AND v.fecha >= @fechaInicio
    AND v.fecha <= @fechaFin
    ORDER BY v.fecha DESC, p.nombre_producto";

    using var cmd = new NpgsqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@fechaInicio", inicio);
    cmd.Parameters.AddWithValue("@fechaFin", fin);

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        ReportLines.Add(new VentaLinea
        {
            Fecha = Convert.ToDateTime(reader.GetValue(1)),
            Producto = Convert.ToString(reader.GetValue(2)) ?? "",
            Cantidad = Convert.ToInt32(reader.GetValue(3)),
            Precio = Convert.ToDecimal(reader.GetValue(4))
        });
    }
}        private void GenerarReporte_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.IsVisible = false;
                Actualizar();
            }
            catch (Exception ex)
            {
                StatusText.Text = "No se pudo generar el reporte. Verifica la conexión a la base de datos.";
                StatusText.IsVisible = true;
                Console.WriteLine(ex);
            }
        }

        private async void GuardarReporte_Click(object? sender, RoutedEventArgs e)
        {
            if (ReportLines.Count == 0)
            {
                StatusText.Text = "No hay datos para guardar. Genera el reporte primero.";
                StatusText.IsVisible = true;
                return;
            }

            var topLevel = GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar Reporte",
                SuggestedFileName = $"reporte_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (file == null)
                return;

            try
            {
                GenerarPdf(file.Path.LocalPath);
                StatusText.Foreground = Avalonia.Media.Brushes.Green;
                StatusText.Text = "Reporte guardado correctamente.";
                StatusText.IsVisible = true;
            }
            catch (Exception ex)
            {
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
                StatusText.Text = "No se pudo guardar el PDF.";
                StatusText.IsVisible = true;
                Console.WriteLine(ex);
            }
        }

        private void GenerarPdf(string path)
        {
            var lines = ReportLines.ToList();
            var total = lines.Sum(l => l.Subtotal);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("REPORTE DE VENTAS")
                        .SemiBold().FontSize(18).AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(10).Text($"Período: {SelectedPeriod}");
                        col.Item().Text($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}");

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Fecha").SemiBold();
                                header.Cell().Text("Producto").SemiBold();
                                header.Cell().Text("Cant.").SemiBold();
                                header.Cell().Text("Precio").SemiBold();
                                header.Cell().Text("Subtotal").SemiBold();
                            });

                            foreach (var line in lines)
                            {
                                table.Cell().Text(line.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Text(line.Producto);
                                table.Cell().Text(line.Cantidad.ToString());
                                table.Cell().Text(line.Precio.ToString("C"));
                                table.Cell().Text(line.Subtotal.ToString("C"));
                            }
                        });

                        col.Item().PaddingTop(15).AlignRight()
                            .Text($"Total: {total:C}").SemiBold().FontSize(14);
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                        });
                });
            }).GeneratePdf(path);
        }

        private void CloseReport_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}