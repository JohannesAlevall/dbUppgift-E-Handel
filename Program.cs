internal class Program
{
    private static void Main(string[] args)
    {
        DbConnection dbConnection = new();
        while (true)
        {
            int shipmentId = 0;
            bool anotherProduct = true;
            char choice = '0';

            Console.Clear();
            Console.WriteLine("Välj vad du vill göra.");
            Console.WriteLine("");
            Console.WriteLine("[1] Registrera en order.");
            Console.WriteLine("[2] Visa statistik för kampanjer.");
            Console.WriteLine("[3] Visa produkter med för lågt varulager i specifikt varuhus.");
            Console.WriteLine("[4] Visa lista av kategorier. Sorterade efter bästsäljare.");
            Console.WriteLine("[5] Visa oskickade ordrar. Sorterade.. Äldst först.");
            Console.WriteLine("[6] Skapa/registrera ny shipment. Returnera ShipmentId");
            Console.WriteLine("[7] Uppdatera order med shipment.");
            Console.WriteLine("[8] Visa snittförsäljning under varje månad.");
            Console.WriteLine("[9] Avsluta programmet.");


            int.TryParse(Console.ReadLine(), out int menuChoice);
            switch (menuChoice)
            {
                case 1:
                    List<int> products = new();
                    List<int> productQuantities = new();

                    Console.Write("Skriv in kundens ID: ");
                    int.TryParse(Console.ReadLine(), out int customerId);
                    DateTime orderDate = DateTime.Now;

                    Console.Write("Skriv in kampanj ID eller lämna tom: ");
                    bool success = int.TryParse(Console.ReadLine(), out int userInputCampaignId);
                    int? marketingCampaignId = null;
                    if (userInputCampaignId != 0)
                    {
                        marketingCampaignId = userInputCampaignId;
                    }

                    while (anotherProduct)
                    {
                        Console.WriteLine("Skriv in produkt ID: ");
                        int.TryParse(Console.ReadLine(), out int productId);
                        products.Add(productId);

                        Console.WriteLine("Kvantitet: ");
                        int.TryParse(Console.ReadLine(), out int quantity);
                        productQuantities.Add(quantity);

                        Console.WriteLine("Vill du lägga till en till produkt? (J/N)");
                        char.TryParse(Console.ReadLine(), out choice);
                        if (choice == 'N' || choice == 'n')
                        {
                            anotherProduct = false;
                        }
                    }
                    dbConnection.PlaceOrder(customerId, orderDate, marketingCampaignId, products, productQuantities);
                    break;

                case 2:
                    foreach (var item in dbConnection.GetCampaignStats())
                    {
                        Console.WriteLine($"Kampanj titel: {item.Title}. Totalspent: {item.TotalSpent}. Totalsales: {item.Sales}. Startdate: {item.StartDate}. Enddate: {item.EndDate}.");
                        Console.WriteLine("");
                    }
                    Console.ReadLine();
                    break;
                case 3:
                    foreach (var item in dbConnection.GetWarehouseLowStockProducts())
                    {
                        Console.WriteLine($"Warehouse address: {item.WarehouseAddress}. Warehouse city: {item.WarehouseCity}. Warehouse contact: {item.WareHouseContact}. Produkt: {item.Product}. Kvantitet: {item.Quantity}. Minimum kvantitet: {item.MinimumQuantity}.");
                        Console.WriteLine($"Supplier: {item.Supplier}. Supplier contact: {item.SupplierContact}");
                        Console.WriteLine("");
                    }
                    Console.ReadLine();
                    break;
                case 4:
                    foreach (var item in dbConnection.GetCategoriesSortedByTotalSales())
                    {
                        Console.WriteLine($"Kategori namn: {item.CategoryTitle}. Total försäljning: {item.CategorySales}");
                        Console.WriteLine("");
                    }
                    Console.ReadLine();
                    break;
                case 5:
                    foreach (var item in dbConnection.GetUnsentOrdersSortedByOldestFirst())
                    {
                        Console.WriteLine($"Order ID: {item.Id}. Order datum: {item.Date}. ProductOrderId: {item.ProductOrderId}. Produkt namn: {item.Title}. Antal beställda: {item.Quantity}. Antal i lager: {item.InStock}");
                        Console.WriteLine("");
                    }
                    Console.ReadLine();
                    break;
                case 6:
                    Console.Write("Vilka levererar paketet?: ");
                    string carrier = Console.ReadLine();
                    Console.Write("Vad är spårningsnumret?: ");
                    string trackingNumber = Console.ReadLine();

                    shipmentId = dbConnection.CreateShipmentReturnId(DateTime.Now, carrier, trackingNumber);
                    break;
                case 7:
                    List<int> productOrderRelationIds = new();
                    while (anotherProduct)
                    {
                        Console.Write("Skriv in ID på productOrderRelation: ");
                        int.TryParse(Console.ReadLine(), out int productOrderRelationId);
                        productOrderRelationIds.Add(productOrderRelationId);

                        Console.WriteLine("Finns det fler produkter i ordern? (J/N)");
                        char.TryParse(Console.ReadLine(), out choice);
                        if (choice == 'N' || choice == 'n')
                        {
                            anotherProduct = false;
                        }
                    }
                    dbConnection.UpdateProductOrderWithShipment(shipmentId, productOrderRelationIds);
                    break;
                case 8:
                    foreach (var item in dbConnection.GetAverageSalesByMonth())
                    {
                        Console.WriteLine($"Månad: {item.Month}. Average sales: {item.AverageSales} SEK.");
                    }
                    Console.ReadLine();
                    break;
                case 9:
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
