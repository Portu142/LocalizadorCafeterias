using LocalizadorCafes.Domain.Entities;
using NetTopologySuite.Geometries;

namespace LocalizadorCafes.Infrastructure.Persistence;

/// <summary>
/// Datos de ejemplo compartidos por el seed de PostgreSQL y por el repositorio
/// en memoria que se usa cuando no hay base de datos disponible.
/// </summary>
public static class DemoData
{
    public static List<Cafe> CreateCafes()
    {
        var central = CreateCafe(
            "Café Central",
            "Calle del Prado 21, 28014 Madrid",
            "Huertas",
            "+34 913 69 14 75",
            "Lun-Vie 08:00-20:00 · Sáb-Dom 09:00-21:00",
            40.4145,
            -3.6965,
            new[] { CafeTags.Wifi, CafeTags.PowerOutlets, CafeTags.Accessible },
            new[]
            {
                ("Café con leche", 2.50m),
                ("Cappuccino", 3.00m),
                ("Croissant", 2.20m),
                ("Tarta de queso", 4.50m)
            });
        AddReview(central, "Laura", 5, "Ambiente increíble y café excelente.", 2026, 10, 12,
            ("Café con leche", 5), ("Tarta de queso", 5));
        AddReview(central, "Carlos", 4, "Buen lugar para trabajar, aunque se llena.", 2026, 9, 28,
            ("Cappuccino", 4), ("Croissant", 4));

        var toma = CreateCafe(
            "Toma Café",
            "Calle de la Palma 49, 28004 Madrid",
            "Malasaña",
            "+34 910 00 11 22",
            "Lun-Vie 08:30-19:00 · Sáb 09:00-20:00 · Dom 10:00-18:00",
            40.4255,
            -3.7045,
            new[] { CafeTags.Wifi, CafeTags.Takeaway, CafeTags.Vegan },
            new[]
            {
                ("Flat white", 3.20m),
                ("Latte", 3.00m),
                ("Avocado toast", 6.50m),
                ("Brownie", 3.50m)
            });
        AddReview(toma, "Marta", 5, "El mejor flat white de Madrid.", 2026, 10, 2,
            ("Flat white", 5), ("Brownie", 4));
        AddReview(toma, "Diego", 5, "Muy buen café y trato cercano.", 2026, 9, 15,
            ("Latte", 5), ("Avocado toast", 4));

        var hanso = CreateCafe(
            "HanSo Café",
            "Calle de San Vicente Ferrer 23, 28004 Madrid",
            "Malasaña",
            "+34 915 22 33 44",
            "Lun-Dom 09:00-21:00",
            40.4260,
            -3.7050,
            new[] { CafeTags.Wifi, CafeTags.PowerOutlets, CafeTags.Vegan, CafeTags.Takeaway },
            new[]
            {
                ("Matcha latte", 3.80m),
                ("Café de especialidad", 2.80m),
                ("Carrot cake", 4.20m),
                ("Brunch", 12.00m)
            });
        AddReview(hanso, "Ana", 5, "Un sitio precioso y el matcha está delicioso.", 2026, 10, 8,
            ("Matcha latte", 5), ("Carrot cake", 5));
        AddReview(hanso, "Pablo", 4, "Buen brunch, algo caro.", 2026, 9, 22,
            ("Brunch", 4), ("Matcha latte", 4));

        var oriente = CreateCafe(
            "Café de Oriente",
            "Plaza de Oriente 2, 28013 Madrid",
            "Palacio",
            "+34 915 41 43 32",
            "Lun-Dom 10:00-23:00",
            40.4175,
            -3.7105,
            new[] { CafeTags.Terrace, CafeTags.Accessible },
            new[]
            {
                ("Café solo", 2.00m),
                ("Cortado", 2.20m),
                ("Bocadillo", 5.50m),
                ("Tarta", 5.00m)
            });
        // Fotos reales de Wikimedia Commons con licencia libre: se sirven desde
        // sus servidores y se muestran siempre con autor, licencia y enlace.
        oriente.Photos.Add(new CafePhoto
        {
            Url = "https://thumb.wikimedia.org/wikipedia/commons/thumb/1/10/Caf%C3%A9_de_Oriente.jpg/960px-Caf%C3%A9_de_Oriente.jpg",
            Author = "DIMSFIKAS",
            License = "CC BY-SA 3.0",
            SourceUrl = "https://commons.wikimedia.org/wiki/File:Caf%C3%A9_de_Oriente.jpg"
        });
        oriente.Photos.Add(new CafePhoto
        {
            Url = "https://thumb.wikimedia.org/wikipedia/commons/thumb/0/01/Picatostes-Caf%C3%A9_de_Oriente_%28Madrid%29.jpg/960px-Picatostes-Caf%C3%A9_de_Oriente_%28Madrid%29.jpg",
            Author = "Tamorlan",
            License = "CC BY 3.0",
            SourceUrl = "https://commons.wikimedia.org/wiki/File:Picatostes-Caf%C3%A9_de_Oriente_(Madrid).jpg",
            CreatedAt = DateTimeOffset.UtcNow.AddSeconds(1)
        });

        AddReview(oriente, "Lucía", 4, "Vistas magníficas y café correcto.", 2026, 9, 30,
            ("Café solo", 4), ("Tarta", 3));
        AddReview(oriente, "Javier", 5, "Perfecto para una tarde tranquila.", 2026, 9, 10,
            ("Cortado", 5), ("Bocadillo", 4));

        var federal = CreateCafe(
            "Federal Café",
            "Plaza de las Comendadoras 9, 28015 Madrid",
            "Conde Duque",
            "+34 915 43 21 10",
            "Lun-Vie 08:00-20:00 · Sáb-Dom 09:00-21:00",
            40.4265,
            -3.7075,
            new[] { CafeTags.Wifi, CafeTags.Terrace, CafeTags.PowerOutlets, CafeTags.PetFriendly },
            new[]
            {
                ("Café con leche", 2.60m),
                ("Eggs benedict", 9.50m),
                ("Pancakes", 7.00m),
                ("Zumo natural", 3.50m)
            });
        AddReview(federal, "Sofía", 5, "El brunch es espectacular.", 2026, 10, 1,
            ("Eggs benedict", 5), ("Pancakes", 5));
        AddReview(federal, "Andrés", 4, "Buen ambiente, ideal para el fin de semana.", 2026, 9, 18,
            ("Café con leche", 4), ("Zumo natural", 4));

        var bicicleta = CreateCafe(
            "La Bicicleta Café",
            "Plaza de San Ildefonso 9, 28004 Madrid",
            "Malasaña",
            "+34 915 21 00 33",
            "Lun-Dom 09:00-22:00",
            40.4235,
            -3.7030,
            new[] { CafeTags.Wifi, CafeTags.Terrace, CafeTags.PetFriendly },
            new[]
            {
                ("Café bombón", 2.40m),
                ("Smoothie", 4.00m),
                ("Tostada", 3.20m),
                ("Brownie", 3.00m)
            });
        AddReview(bicicleta, "Elena", 4, "Muy acogedor y con buena música.", 2026, 9, 25,
            ("Café bombón", 4), ("Tostada", 4));
        AddReview(bicicleta, "Raúl", 4, "Buen sitio para merendar.", 2026, 9, 5,
            ("Brownie", 3), ("Smoothie", 4));

        return new List<Cafe> { central, toma, hanso, oriente, federal, bicicleta };
    }

    private static Cafe CreateCafe(
        string name,
        string address,
        string neighborhood,
        string phone,
        string hours,
        double latitude,
        double longitude,
        IEnumerable<string> tags,
        IEnumerable<(string Name, decimal Price)> menu)
    {
        var cafe = new Cafe
        {
            Name = name,
            Address = address,
            Neighborhood = neighborhood,
            Phone = phone,
            Hours = hours,
            Location = new Point(longitude, latitude) { SRID = 4326 },
            Tags = tags.ToList()
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

    private static void AddReview(
        Cafe cafe,
        string author,
        int rating,
        string comment,
        int year,
        int month,
        int day,
        params (string Item, int Score)[] itemRatings)
    {
        var review = new Review
        {
            Author = author,
            Rating = rating,
            Comment = comment,
            CreatedAt = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero)
        };

        foreach (var (itemName, score) in itemRatings)
        {
            var menuItem = cafe.Menu.First(item => item.Name == itemName);
            review.MenuItemRatings.Add(new MenuItemRating
            {
                MenuItemId = menuItem.Id,
                Score = score
            });
        }

        cafe.Reviews.Add(review);
    }
}
