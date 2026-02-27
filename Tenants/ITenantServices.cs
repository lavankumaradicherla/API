using Microsoft.AspNetCore.Mvc;
using MRV_API.Models;
using MRV_API.Services.Tenants.Dto;

namespace MRV_API.Services.Tenants
{
    public interface ITenantServices
    {
        Task<ActionResult<IEnumerable<Tenant>>> GetAllAsync();

        Task<Tenant?> GetTenantByIdAsync(int id);

        Task<(bool Success, int StatusCode,string Message, Tenant? Tenant, Exception? Exception)> CreateTenant([FromBody] TenantCreateDto tenantDto);

        Task DeleteAsync(int id);

        Task<bool> ExistsAsync(int id);

        Task<(bool Success, int StatusCode, string Message, Tenant? Tenant, Exception? Exception)> UpdateAsync(int id, [FromBody] TenantUpdateDto tenantDto);


    }
}
