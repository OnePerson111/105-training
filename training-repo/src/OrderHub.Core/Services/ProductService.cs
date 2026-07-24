using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold)
    {
        var products = await _productRepository.GetLowStockActiveAsync(threshold);

        var since = DateTime.UtcNow.AddDays(-30);
        var sold = await _orderRepository.GetSoldQuantitiesSinceAsync(since, products.Select(p => p.Id));

        return products
            .Select(p => new LowStockProduct(
                p.Sku,
                p.Name,
                p.StockQuantity,
                sold.TryGetValue(p.Id, out var quantity) ? quantity : 0))
            .ToList();
    }
}
