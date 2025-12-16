namespace Store.Shared.DTOs
{
    public class ProductResult
    {
        public List<ProductDTO> Products { get; set; } = new List<ProductDTO>();
        public int TotalCount { get; set; }
    }
}
