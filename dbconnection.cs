using Dapper;
using System.Data;
using System.Data.SqlClient;

class DbConnection
{
    private static IDbConnection CreateConnection()
    {
        return new SqlConnection("Server=localhost,1433;User=sa;Password=apA123!#!;Database=ECommerce;");
    }

    public void PlaceOrder(int customerId, DateTime orderDate, int? marketingCampaignId, List<int> productIds, List<int> quantities)
    {
        using (var connection = CreateConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    string orderInsert = @"INSERT INTO [Order]([CustomerId], [Date], [MarketingCampaignId])
                                       VALUES(@customerId, @orderDate, @marketingCampaignId);
                                       SELECT CAST(SCOPE_IDENTITY() as int);";
                    int orderId = connection.QuerySingle<int>(orderInsert, new { customerId, orderDate, marketingCampaignId }, transaction);

                    for (int i = 0; i < productIds.Count; i++)
                    {
                        string productOrderInsert = @"INSERT INTO ProductOrderRelation([OrderId], [ProductId], [Quantity])
                                                  VALUES(@orderId, @productId, @quantity);";
                        connection.Execute(productOrderInsert, new { orderId, productId = productIds[i], quantity = quantities[i] }, transaction);
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    public List<dynamic> GetCampaignStats()
    {
        using (var connection = CreateConnection())
        {
            string query = @"SELECT MarketingCampaign.Title, SUM(DATEDIFF(day, MarketingCampaign.StartDate, COALESCE(MarketingCampaign.EndDate, GETDATE())) * MarketingCampaign.DailyBudget) as TotalSpent, SUM(Product.Price * ProductOrderRelation.Quantity) As Sales, MarketingCampaign.[StartDate], MarketingCampaign.[EndDate] FROM [Order]
                            JOIN MarketingCampaign on [Order].MarketingCampaignId = MarketingCampaign.Id
                            JOIN ProductOrderRelation on [Order].Id = ProductOrderRelation.OrderId
                            JOIN Product on ProductOrderRelation.ProductId = Product.Id
                            GROUP BY marketingCampaign.Title, MarketingCampaign.StartDate, MarketingCampaign.EndDate;";
            IEnumerable<dynamic> result = connection.Query<dynamic>(query);
            return result.ToList();
        }
    }
    public List<dynamic> GetWarehouseLowStockProducts()
    {
        using (var connection = CreateConnection())
        {
            string query = @"SELECT Warehouse.Address as WarehouseAddress, Warehouse.City as WarehouseCity, Warehouse.ContactNumber as WareHouseContact, Product.Title as Product, ProductWarehouseRelation.Quantity, ProductWarehouseRelation.MinimumQuantity, Supplier.Name as Supplier, Supplier.ContactNumber as SupplierContact FROM Product
                            JOIN ProductWarehouseRelation on Product.Id = ProductWarehouseRelation.ProductId
                            JOIN Warehouse on ProductWarehouseRelation.WarehouseId = Warehouse.Id
                            JOIN Supplier on Product.SupplierId = Supplier.Id
                            WHERE ProductWarehouseRelation.Quantity < ProductWarehouseRelation.MinimumQuantity;";
            IEnumerable<dynamic> result = connection.Query<dynamic>(query);
            return result.ToList();
        }
    }
    public List<dynamic> GetCategoriesSortedByTotalSales()
    {
        using (var connection = CreateConnection())
        {
            string query = @"SELECT Category.Title as CategoryTitle, SUM(Product.Price * ProductOrderRelation.Quantity) as CategorySales FROM Product
                            JOIN ProductOrderRelation on Product.Id = ProductOrderRelation.ProductId
                            JOIN [Order] on ProductOrderRelation.OrderId = [Order].[Id]
                            JOIN ProductCategoryRelation on Product.Id = ProductCategoryRelation.ProductId
                            JOIN Category on ProductCategoryRelation.CategoryId = Category.Id
                            GROUP BY Category.Id, Category.Title
                            ORDER BY CategorySales DESC;";
            IEnumerable<dynamic> result = connection.Query<dynamic>(query);
            return result.ToList();
        }
    }
    public List<dynamic> GetAverageSalesByMonth()
    {
        using (var connection = CreateConnection())
        {
            string query = @"SELECT DATENAME(MONTH, [Order].[Date]) as Month, AVG(Product.Price * ProductOrderRelation.Quantity) as AverageSales FROM [Order]
                            JOIN ProductOrderRelation on [Order].Id = ProductOrderRelation.OrderId
                            JOIN Product on ProductOrderRelation.ProductId = Product.Id
                            GROUP BY DATENAME(MONTH, [Order].[Date])
                            ORDER BY AverageSales DESC;";
            IEnumerable<dynamic> result = connection.Query<dynamic>(query);
            return result.ToList();
        }
    }
    public List<dynamic> GetUnsentOrdersSortedByOldestFirst()
    {
        using (var connection = CreateConnection())
        {
            string query = @"SELECT [Order].Id,[Order].Date,ProductOrderRelation.Id as ProductOrderId, Product.Title, ProductOrderRelation.Quantity, ProductWarehouseRelation.Quantity as InStock FROM Product
                            JOIN ProductWarehouseRelation on Product.Id = ProductWarehouseRelation.ProductId
                            JOIN Warehouse on ProductWarehouseRelation.WarehouseId = Warehouse.Id
                            JOIN ProductOrderRelation on Product.Id = ProductOrderRelation.ProductId
                            JOIN [Order] on ProductOrderRelation.OrderId = [Order].Id
                            WHERE ProductOrderRelation.ShipmentId is NULL
                            ORDER BY [Order].Id ASC;";
            IEnumerable<dynamic> result = connection.Query<dynamic>(query);
            return result.ToList();
        }
    }
    public int CreateShipmentReturnId(DateTime shipDate, string carrier, string trackingNumber)
    {
        using (var connection = CreateConnection())
        {
            string insertStatement = @"INSERT INTO Shipment(ShipDate, Carrier, TrackingNumber)
                                    VALUES(@shipDate, @carrier, @trackingNumber);
                                    SELECT CAST(SCOPE_IDENTITY() as int);";


            int shipmentId = connection.QuerySingle(insertStatement, new { shipDate, carrier, trackingNumber });
            return shipmentId;
        }
    }
    public void UpdateProductOrderWithShipment(int shipmentId, List<int> listProductOrderRelationId)
    {
        using (var connection = CreateConnection())
        {
            foreach (int productOrderRelationId in listProductOrderRelationId)
            {
                string insertStatement = @"UPDATE ProductOrderRelation
                                    SET ShipmentId = @shipmentId
                                    WHERE [ProductOrderRelation].[Id] = @productOrderRelationId;";

                connection.Execute(insertStatement, new { shipmentId, productOrderRelationId });
            }
        }
    }
}