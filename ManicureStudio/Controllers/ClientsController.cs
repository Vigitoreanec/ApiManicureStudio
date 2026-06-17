using ManicureStudio.API.APIResult;
using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManicureStudio.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(IClientRepository clientRepository,
                                 ILogger<ClientsController> logger)
        {
            _clientRepository = clientRepository;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Client>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _clientRepository.GetAllAsync();
            _logger.LogInformation("Запрошен список клиентов. Найдено: {Count}", clients.Count());
            return Ok(ApiResult<IEnumerable<Client>>.Success(clients));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);

            if (client == null)
            {
                return NotFound(ApiResult<Client>.Failure($"Клиент с ID={id} не найден"));
            }

            return Ok(ApiResult<Client>.Success(client));
        }

        [HttpGet("phone/{phone}")]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByPhone(string phone)
        {
            var client = await _clientRepository.GetByPhoneAsync(phone);

            if (client == null)
            {
                return NotFound(ApiResult<Client>.Failure($"Клиент с телефоном {phone} не найден"));
            }

            return Ok(ApiResult<Client>.Success(client));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Client client)
        {
            var newClient = await _clientRepository.GetByPhoneAsync(client.PhoneNumber);
            if (newClient != null)
            {
                return BadRequest(ApiResult<Client>.Failure(
                    "Ошибка создания клиента",
                    $"Клиент с телефоном {client.PhoneNumber} уже существует"));
            }

            
            var created = await _clientRepository.AddAsync(client);
            await _clientRepository.SaveChangesAsync();

            _logger.LogInformation("Создан новый клиент: {FirstName} {LastName}, ID={Id}",
            created.FirstName, created.LastName, created.Id);

            return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResult<Client>.Success(created, "Клиент успешно создан"));
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(ApiResult.Failure($"Клиент с ID={id} не найден"));
            }
            await _clientRepository.SoftDeleteAsync(id);
            await _clientRepository.SaveChangesAsync();
            _logger.LogInformation("Клиент ID={Id} помечен как удалённый", id);

            return Ok(ApiResult.Ok($"Клиент ID={id} успешно удалён"));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResult<Client>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Client client)
        {
            
            var existing = await _clientRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(ApiResult<Client>.Failure("Ошибка обновления", $"Клиент с ID {id} не найден."));
            }

            if (existing.PhoneNumber != client.PhoneNumber)
            {
                var clientWithSamePhone = await _clientRepository.GetByPhoneAsync(client.PhoneNumber);
                if (clientWithSamePhone != null)
                {
                    return BadRequest(ApiResult<Client>.Failure(
                        "Ошибка обновления",
                        $"Клиент с телефоном {client.PhoneNumber} уже существует."));
                }
            }

            existing.FirstName = client.FirstName;
            existing.LastName = client.LastName;
            existing.PhoneNumber = client.PhoneNumber;
            existing.Email = client.Email;
            existing.BirthDate = client.BirthDate;
            existing.Notes = client.Notes;
            existing.IsVip = client.IsVip;

            existing.UpdatedAt = DateTime.Now; // Фиксируем время изменения
            existing.Appointments = client.Appointments;
            

            await _clientRepository.UpdateAsync(existing);
            await _clientRepository.SaveChangesAsync();

            _logger.LogInformation("Обновлен клиент: {FirstName} {LastName}, ID={Id}",
                                   existing.FirstName,
                                   existing.LastName,
                                   existing.Id);

            return Ok(ApiResult<Client>.Success(existing, "Данные клиента обновлены"));
        }
    }
}
