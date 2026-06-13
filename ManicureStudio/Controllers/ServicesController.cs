using ManicureStudio.API.APIResult;
using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ManicureStudio.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(IServiceRepository serviceRepository,
                                  ILogger<ServicesController> logger)
        {
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Service>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var services = await _serviceRepository.GetActiveServicesWithCategoriesAsync();
            _logger.LogInformation("Запрошен список Услуг. Найдено: {Count}", services.Count());
            return Ok(ApiResult<IEnumerable<Service>>.Success(services));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var service = await _serviceRepository.GetByIdAsync(id);
            if (service == null)
                return NotFound(ApiResult<Service>.Failure($"Услуга с ID={id} не найдена"));

            return Ok(ApiResult<Service>.Success(service));
        }

        [HttpGet("category/{categoryId:int}")]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var services = await _serviceRepository.GetByCategoryAsync(categoryId);
            if (services == null)
                return NotFound(ApiResult<Service>.Failure($"Услуги по категории не найдены"));
            _logger.LogInformation("Запрошен список Услуг, по категории {categoryId}. Найдено: {Count}",categoryId, services.Count());
            return Ok(ApiResult<IEnumerable<Service>>.Success(services));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Service service)
        {
            var created = await _serviceRepository.AddAsync(service);
            await _serviceRepository.SaveChangesAsync();

            _logger.LogInformation("Создана услуга: {Name}, цена: {Price} руб.", created.Name, created.Price);

            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResult<Service>.Success(created, "Услуга успешно создана"));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Service>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Service service)
        {
            var existing = await _serviceRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResult<Service>.Failure($"Услуга с ID={id} не найдена"));
            
            existing.Name = service.Name;
            existing.Description = service.Description;
            existing.Price = service.Price;
            existing.DurationMinutes = service.DurationMinutes;
            existing.CategoryId = service.CategoryId;
            existing.IsActive = service.IsActive;
            existing.Category = service.Category;

            await _serviceRepository.UpdateAsync(existing);
            await _serviceRepository.SaveChangesAsync();
            _logger.LogInformation("Услуга: {Name} Изменена", existing.Name);

            return Ok(ApiResult<Service>.Success(existing, "Услуга обновлена"));
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _serviceRepository.GetByIdAsync(id);
            if (service == null)
                return NotFound(ApiResult.Failure($"Услуга с ID={id} не найдена"));

            await _serviceRepository.SoftDeleteAsync(id);
            await _serviceRepository.SaveChangesAsync();

            _logger.LogInformation("Услуга: {Name} удалена", service.Name);

            return Ok(ApiResult.Ok($"Услуга ID={id} удалена"));
        }
    }
}
