namespace MRV_API.Services.Tenants.Dto
{
    public class TenantCreateDto
    {
        public int TenantID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class TenantUpdateDto
    {
        public int TenantID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

}
