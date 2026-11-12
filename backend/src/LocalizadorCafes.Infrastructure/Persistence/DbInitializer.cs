using LocalizadorCafes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Cafes.AnyAsync(cancellationToken))
        {
            return;
        }

        var central = CreateCafe(
            "Café Central",
            "Calle del Prado 21, 28014 Madrid",
            "Huertas",
            "+34 913 69 14 75",
            "Lun-Vie 08:00-20:00 · Sáb-Dom 09:00-21:00",
            40.4145,
            -3.6965,
            new[]
            {
                ("Café con leche", 2.50m),
                ("Cappuccino", 3.00m),
                ("Croissant", 2.20m),
                ("Tarta de queso", 4.50m)
            });
        central.Reviews.Add(CreateReview("Laura", 5, "Ambiente increíble y café excelente.", 2026, 10, 12));
        central.Reviews.Add(CreateReview("Carlos", 4, "Buen lugar para trabajar, aunque se llena.", 2026, 9, 28));

        var toma = CreateCafe(
            "Toma Café",
            "Calle de la Palma 49, 28004 Madrid",
            "Malasaña",
            "+34 910 00 11 22",
            "Lun-Vie 08:30-19:00 · Sáb 09:00-20:00 · Dom 10:00-18:00",
            40.4255,
            -3.7045,
            new[]
            {
                ("Flat white", 3.20m),
                ("Latte", 3.00m),
                ("Avocado toast", 6.50m),
                ("Brownie", 3.50m)
            });
        toma.Reviews.Add(CreateReview("Marta", 5, "El mejor flat white de Madrid.", 2026, 10, 2));
        toma.Reviews.Add(CreateReview("Diego", 5, "Muy buen café y trato cercano.", 2026, 9, 15));

        var hanso = CreateCafe(
            "HanSo Café",
            "Calle de San Vicente Ferrer 23, 28004 Madrid",
            "Malasaña",
            "+34 915 22 33 44",
            "Lun-Dom 09:00-21:00",
            40.4260,
            -3.7050,
            new[]
            {
                ("Matcha latte", 3.80m),
                ("Café de especialidad", 2.80m),
                ("Carrot cake", 4.20m),
                ("Brunch", 12.00m)
            });
        hanso.Reviews.Add(CreateReview("Ana", 5, "Un sitio precioso y el matcha está delicioso.", 2026, 10, 8));
        hanso.Reviews.Add(CreateReview("Pablo", 4, "Buen brunch, algo caro.", 2026, 9, 22));

        var oriente = CreateCafe(
            "Café de Oriente",
            "Plaza de Oriente 2, 28013 Madrid",
            "Palacio",
            "+34 915 41 43 32",
            "Lun-Dom 10:00-23:00",
            40.4175,
            -3.7105,
            new[]
            {
                ("Café solo", 2.00m),
                ("Cortado", 2.20m),
                ("Bocadillo", 5.50m),
                ("Tarta", 5.00m)
            });
        oriente.Reviews.Add(CreateReview("Lucía", 4, "Vistas magníficas y café correcto.", 2026, 9, 30));
        oriente.Reviews.Add(CreateReview("Javier", 5, "Perfecto para una tarde tranquila.", 2026, 9, 10));

        var federal = CreateCafe(
            "Federal Café",
            "Plaza de las Comendadoras 9, 28015 Madrid",
            "Conde Duque",
            "+34 915 43 21 10",
            "Lun-Vie 08:00-20:00 · Sáb-Dom 09:00-21:00",
            40.4265,
            -3.7075,
            new[]
            {
                ("Café con leche", 2.60m),
                ("Eggs benedict", 9.50m),
                ("Pancakes", 7.00m),
                ("Zumo natural", 3.50m)
            });
        federal.Reviews.Add(CreateReview("Sofía", 5, "El brunch es espectacular.", 2026, 10, 1));
        federal.Reviews.Add(CreateReview("Andrés", 4, "Buen ambiente, ideal para el fin de semana.", 2026, 9, 18));

        var bicicleta = CreateCafe(
            "La Bicicleta Café",
            "Plaza de San Ildefonso 9, 28004 Madrid",
            "Malasaña",
            "+34 915 21 00 33",
            "Lun-Dom 09:00-22:00",
            40.4235,
            -3.7030,
            new[]
            {
                ("Café bombón", 2.40m),
                ("Smoothie", 4.00m),
                ("Tostada", 3.20m),
                ("Brownie", 3.00m)
            });
        bicicleta.Reviews.Add(CreateReview("Elena", 4, "Muy acogedor y con buena música.", 2026, 9, 25));
        bicicleta.Reviews.Add(CreateReview("Raúl", 4, "Buen sitio para merendar.", 2026, 9, 5));

        dbContext.Cafes.AddRange(central, toma, hanso, oriente, federal, bicicleta);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Cafe CreateCafe(
        string name,
        string address,
        string neighborhood,
        string phone,
        string hours,
        double latitude,
        double longitude,
        IEnumerable<(string Name, decimal Price)> menu)
    {
        var cafe = new Cafe
        {
            Name = name,
            Address = address,
            Neighborhood = neighborhood,
            Phone = phone,
            Hours = hours,
            Location = new Point(longitude, latitude) { SRID = 4326 }
        };

        foreach (var item in menu)
        {
            cafe.Menu.Add(new MenuItem
            {
                Name = item.Name,
                Price = item.Price
            });
        }

        return cafe;
    }

    private static Review CreateReview(
        string author,
        int rating,
        string comment,
        int year,
        int month,
        int day)
    {
        return new Review
        {
            Author = author,
            Rating = rating,
            Comment = comment,
            CreatedAt = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero)
        };
    }
}
