using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRV_API.EntityFrameworkCore;
using MRV_API.Models;
using MRV_API.Services.Tenants.Dto;

namespace MRV_API.Services.Tenants
{
    public class TenantService : ITenantServices
    {
        private readonly MRVDbContext _dbContext;
        private readonly ILogger<TenantService> _logger;
       
        public TenantService(ILogger<TenantService> logger, MRVDbContext context)
        {
            _logger = logger;
            _dbContext = context;
        }


        public async Task<(bool Success, int StatusCode, string Message, Tenant? Tenant, Exception? Exception)> CreateTenant(TenantCreateDto tenantDto)
        {
            _logger.LogInformation("Received request to create a new tenant.");

            try
            {
                // Check if the tenant name already exists
                var existingTenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Name == tenantDto.Name);
                if (existingTenant != null)
                {
                    _logger.LogWarning($"Tenant creation failed. Tenant with name '{tenantDto.Name}' already exists.");
                    return (false, 409, "Tenant already exists.", null, null); // 409 Conflict
                }


                // hello
                // hello2

                // Create a new Tenant entity
                var tenant = new Tenant
                {
                    Name = tenantDto.Name,
                    Description = tenantDto.Description,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _dbContext.Tenants.Add(tenant);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Tenant '{tenant.Name}' created successfully with ID {tenant.TenantID}.");

                // Fetch roles where TenantID is null (template roles)
                var roles = await _dbContext.Roles
                    .Where(r => !r.IsGlobal)
                    .Include(r => r.RolePermissions)
                    .ToListAsync();

                TenantRole tenantAdminRole = null;

                foreach (var role in roles)
                {
                    // Create a new TenantRole for the current tenant
                    var newRole = new TenantRole
                    {
                        TenantID = tenant.TenantID, // Associate with the newly created tenant
                        RoleName = role.RoleName,
                        Description = role.Description,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        TenantRolePermissions = role.RolePermissions.Select(rp => new TenantRolePermission
                        {
                            PermissionID = rp.PermissionID,
                            IsActive = true,
                            AssignedDate = DateTime.UtcNow
                        }).ToList()
                    };

                    _dbContext.TenantRoles.Add(newRole);

                    // Save the TenantAdmin role for later use
                    if (role.RoleName == "Tenant Admin")
                    {
                        tenantAdminRole = newRole;
                    }
                }

                // Save all roles and permissions
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Roles for tenant '{tenant.Name}' created successfully.");

                if (tenantAdminRole != null)
                {
                    var tenantAdminUser = new User
                    {
                        FirstName = tenantDto.Name,
                        LastName = "Admin",
                        Email = $"{tenantDto.Name.ToLower()}admin@example.com",
                        PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92",
                        TenantID = tenant.TenantID,
                        LoginType = "AdminLoginType",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        IsActive = true,
                        UserRole = tenantAdminRole.RoleName,
                        RoleId=tenantAdminRole.TenantRoleID,
                        EntityID = null
                    };

                    _dbContext.Users.Add(tenantAdminUser);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"TenantAdmin user created for tenant '{tenant.Name}'.");
                }

                return (true, 201, "Tenant created successfully.", tenant, null); // 201 Created
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while creating the tenant.");
                return (false, 500, "A database error occurred while creating the tenant.", null, dbEx); // 500 Internal Server Error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating the tenant.");
                return (false, 500, "An unexpected error occurred while creating the tenant.", null, ex); // 500 Internal Server Error
            }
        }


        public async Task<ActionResult<IEnumerable<Tenant>>> GetAllAsync()
        {
            return await _dbContext.Tenants
                              .Where(t => t.IsActive == true) // Filter out soft deleted tenants
                              .ToListAsync();
        }

        public async Task<Tenant?> GetTenantByIdAsync(int id)
        {
            try
            {
                return await _dbContext.Tenants
                    .FirstOrDefaultAsync(t => t.TenantID == id);
            }
            catch (Exception)
            {
                throw new Exception("An error occurred while retrieving the tenant.");
            }
        }
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _dbContext.Tenants.AnyAsync(e => e.TenantID == id);
            }
            catch (Exception ex)
            {
                // Log exception
                throw new Exception($"An error occurred while checking existence for the Tenants with ID {id}.", ex);
            }
        }
        public async Task DeleteAsync(int id)
        {
            try
            {
                var tenant = await _dbContext.Tenants.FindAsync(id);
                if (tenant != null)
                {
                    // Mark the tenant as soft deleted
                    tenant.IsActive = false;
                    tenant.UpdatedDate = DateTime.UtcNow; // Optional: log the deletion date

                    // Update the tenant in the database
                    _dbContext.Tenants.Update(tenant);

                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw new Exception($"An error occurred while deleting the Tenants with ID {id}.", ex);
            }
        }
        public async Task<(bool Success, int StatusCode, string Message, Tenant? Tenant, Exception? Exception)> UpdateAsync(int id, TenantUpdateDto tenantDto)

        {
            _logger.LogInformation("Received request to update a new tenant.");

            try
            {
                // Check if the tenant name already exists
                var existingTen = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Name == tenantDto.Name);
                if (existingTen != null)
                {
                    _logger.LogWarning($"Tenant updation failed. Tenant with name '{tenantDto.Name}' already exists.");
                    return (false, 409, "Tenant already exists.", null, null); // 409 Conflict
                }
                var existingTenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.TenantID == id);

                existingTenant.Name = tenantDto.Name;
                existingTenant.Description = tenantDto.Description;
                existingTenant.UpdatedDate = DateTime.UtcNow; // Update the timestamp
                existingTenant.IsActive = tenantDto.IsActive;
                _dbContext.Entry(existingTenant).State = EntityState.Modified;
                _dbContext.Entry(existingTenant).Property(e => e.CreatedDate).IsModified = false;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Tenant '{tenantDto.Name}' updated successfully with ID {tenantDto.TenantID}.");

                
               return (true, 201, "Tenant updated successfully.", existingTenant, null); // 201 Created
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while updating the tenant.");
                return (false, 500, "A database error occurred while updating the tenant.", null, dbEx); // 500 Internal Server Error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating the tenant.");
                return (false, 500, "An unexpected error occurred while updating the tenant.", null, ex); // 500 Internal Server Error
            }

        }
    }
}
