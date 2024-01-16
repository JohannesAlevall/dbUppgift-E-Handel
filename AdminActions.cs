using Dapper;
using System.Data;
using System.Data.SqlClient;

class AdminActions
{
    static IDbConnection dbConnection = new SqlConnection("Server=localhost,1433;User=sa;Password=apA123!#!;Database=ECommerce;");

    private static void Open()
    {
        if (dbConnection.State != ConnectionState.Open)
        { dbConnection.Open(); }
    }

    public List<dynamic> GetCategoriesSortedBySales()
    {
        Open();
        string query = @"
        SELECT Category.Id as CategoryId, Category.Title as CategoryTitle, SUM(Product.Price * ProductOrderRelation.Quantity) as CategorySales FROM Product
        JOIN ProductOrderRelation on Product.Id = ProductOrderRelation.ProductId
        JOIN [Order] on ProductOrderRelation.OrderId = [Order].[Id]
        JOIN ProductCategoryRelation on Product.Id = ProductCategoryRelation.ProductId
        JOIN Category on ProductCategoryRelation.CategoryId = Category.Id
        GROUP BY Category.Id, Category.Title
        ORDER BY CategorySales DESC";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query);

        return result.ToList();
    }
    public dynamic GetSalesFromSpecificCampaign(string marketingCampaignId)
    {
        Open();
        string query = @"SELECT SUM(Product.Price * ProductOrderRelation.Quantity) FROM [Order]
        JOIN MarketingCampaign on [Order].MarketingCampaignId = MarketingCampaign.Id
        JOIN ProductOrderRelation on [Order].Id = ProductOrderRelation.OrderId
        JOIN Product on ProductOrderRelation.ProductId = Product.Id
        WHERE MarketingCampaign.Id = @marketingCampaignId;";


        return dbConnection.QueryFirstOrDefault<dynamic>(query, new { MarketingCampaignId = marketingCampaignId });
    }





    public List<dynamic> GetUnsentOrders()
    {
        string query = @"SELECT DISTINCT [Order].Id, [Order].[Date] FROM [Order]
        JOIN ProductOrderRelation on [Order].Id = ProductOrderRelation.OrderId
        WHERE ProductOrderRelation.ShipmentId is NULL";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query);

        return result.ToList();
    }
    public List<dynamic> GetOrderedProducts(int orderId)
    {
        string query = @"SELECT Product.Title, ProductOrderRelation.Quantity, ProductOrderRelation.Quantity as InStock FROM PRODUCT
        JOIN ProductOrderRelation on Product.Id = ProductOrderRelation.ProductId
        JOIN [Order] on ProductOrderRelation.OrderId = [Order].Id
        WHERE [Order].Id = @orderId;";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query, new { OrderId = orderId });

        return result.ToList();
    }

    public List<dynamic> GetShippingInformation23(int orderId)
    {
        string query = @"SELECT Customer.FirstName, Customer.LastName, Customer.Email, Customer.PhoneNumber, Customer.City, Customer.ZipCode, Customer.Address FROM Customer
        JOIN [Order] ON Customer.Id = [Order].CustomerId
        WHERE [Order].Id = @orderId;";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query, new { OrderId = orderId });
        return result.ToList();
    }
    public List<dynamic> GetCampaignOrders(int marketingCampaignId)
    {
        string query = @"SELECT [Order].Id, Product.Title, Product.Price FROM [Order]
        JOIN ProductOrderRelation on [Order].Id = ProductOrderRelation.OrderId
        JOIN Product on ProductOrderRelation.ProductId = Product.Id
        WHERE MarketingCampaignId = @marketingCampaignId";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query, new { marketingCampaignId = marketingCampaignId });
        return result.ToList();
    }
    public List<dynamic> GetCampaignSaleNumbers(int marketingCampaignId)
    {
        string query = @"SELECT Sum(Product.Price * ProductOrderRelation.Quantity) as TotalSales FROM [Order]
        JOIN ProductOrderRelation on [Order].Id = ProductOrderRelation.OrderId
        JOIN Product on ProductOrderRelation.ProductId = Product.Id
        JOIN MarketingCampaign ON [Order].MarketingCampaignId = MarketingCampaign.Id
        WHERE MarketingCampaignId = @marketingCampaignId";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query, new { marketingCampaignId = marketingCampaignId });
        return result.ToList();
    }
    public List<dynamic> GetCustomerOrders(int customerId)
    {
        string query = @"SELECT * FROM [Order] 
        WHERE CustomerId = @customerId";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query, new { CustomerId = customerId });
        return result.ToList();
    }
    public List<dynamic> GetCustomersWithZeroOrders()
    {
        Open();

        string query = @"SELECT Customer.FirstName, Customer.LastName, Customer.BirthDate, Customer.Email, Customer.PhoneNumber, Customer.City, Customer.ZipCode, Customer.Id 
                     FROM Customer
                     LEFT JOIN [Order] ON Customer.Id = [Order].[CustomerId]
                     WHERE [Order].Id IS NULL";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query);

        return result.ToList();
    }
    public List<dynamic> GetLowStockProductsInWarehouse(int warehouseId)
    {
        string query = @"SELECT Product.Title, Product.Description, Product.Price, ProductWarehouseRelation.Quantity, ProductWarehouseRelation.MinimumQuantity FROM PRODUCT
        JOIN ProductWarehouseRelation on Product.Id = ProductWarehouseRelation.Id
        JOIN Warehouse on ProductWarehouseRelation.Id = Warehouse.id
        JOIN Supplier on Product.SupplierId = Supplier.id
        WHERE ProductWareHouseRelation.Quantity < ProductWarehouseRelation.minimumquantity AND Warehouse.Id = @warehouseId";

        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query, new { WarehouseId = warehouseId });
        return result.ToList();
    }

    public int RegisterShipmentReturnId(string shipDate, string carrier, string trackingNumber)
    {
        using (var localConnection = new SqlConnection("Server=localhost,1433;User=sa;Password=apA123!#!;Database=ECommerce;"))
        {
            string insertStatement = @"INSERT INTO Shipment (ShipDate, Carrier, TrackingNumber)
                                    VALUES(@ShipDate, @Carrier, @TrackingNumber);
                                    SELECT CAST(SCOPE_IDENTITY() as int);";

            var parameters = new
            {
                ShipDate = shipDate,
                Carrier = carrier,
                TrackingNumber = trackingNumber
            };

            localConnection.Open();
            int shipmentId = localConnection.QuerySingle<int>(insertStatement, parameters);
            return shipmentId;
        }
    }
    public void LinkShipmentToOrder(int shipmentId, int orderId)
    {
        using (var localConnection = new SqlConnection("Server=localhost,1433;User=sa;Password=apA123!#!;Database=ECommerce;"))
        {
            string updateStatement = @"UPDATE ProductOrderRelation
                                    SET ShipmentId = @ShipmentId
                                    WHERE OrderId = @OrderId;";

            localConnection.Open();
            localConnection.Execute(updateStatement, new { ShipmentId = shipmentId, OrderId = orderId });
        }
    }
    public dynamic GetOldestOrderPlusDetails()
    {
        Open();
        string query = @"    SELECT TOP 1 [Order].Id as OrderId, [Order].Date, Title, ProductOrderRelation.Quantity, ProductWarehouseRelation.Quantity as InStock FROM PRODUCT
                    JOIN ProductWarehouseRelation on Product.Id = ProductWarehouseRelation.ProductId
                    JOIN Warehouse on ProductWarehouseRelation.WarehouseId = Warehouse.Id
                    JOIN ProductOrderRelation on Product.Id = ProductOrderRelation.ProductId
                    JOIN [Order] on ProductOrderRelation.OrderId = [Order].Id
                    WHERE ProductOrderRelation.ShipmentId is NULL
                    ORDER BY [Order].Date ASC;";


        return dbConnection.QueryFirstOrDefault<dynamic>(query);
    }

    public dynamic GetShippingInformation(int orderId)
    {
        string query = @"SELECT Customer.FirstName, Customer.LastName, Customer.Email, Customer.PhoneNumber, Customer.City, Customer.ZipCode, Customer.Address FROM Customer
        JOIN [Order] ON Customer.Id = [Order].CustomerId
        WHERE [Order].Id = @orderId;";

        return dbConnection.QueryFirstOrDefault<dynamic>(query, new { OrderId = orderId });
    }

    public List<dynamic> GetSortedUnsentOrdersFirstDate()
    {
        Open();

        string query = @"SELECT [Order].Date, Product.Title, ProductOrderRelation.Quantity, ProductWarehouseRelation.Quantity as InStock FROM Product
        JOIN ProductWarehouseRelation on Product.Id = ProductWarehouseRelation.ProductId
        JOIN Warehouse on ProductWarehouseRelation.WarehouseId = Warehouse.Id
        JOIN ProductOrderRelation on Product.Id = ProductOrderRelation.ProductId
        JOIN [Order] on ProductOrderRelation.OrderId = [Order].Id
        WHERE ProductOrderRelation.ShipmentId is NULL
        ORDER BY [Order].Date ASC;";


        IEnumerable<dynamic> result = dbConnection.Query<dynamic>(query);
        return result.ToList();
    }
}