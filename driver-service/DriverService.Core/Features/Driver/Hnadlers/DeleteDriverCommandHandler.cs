using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;

namespace DriverService.Core.Features.Driver.Handlers
{
    public class DeleteDriverCommandHandler
        : IRequestHandler<DeleteDriverCommand, bool>
    {
        private readonly IDriverRepository _repository;

        public DeleteDriverCommandHandler(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            DeleteDriverCommand request,
            CancellationToken cancellationToken)
        {
            var driver = await _repository.GetByIdAsync(request.DriverId);
            if (driver == null)
                return false;

            await _repository.DeleteAsync(request.DriverId);
            await _repository.SaveChangesAsync();

            return true;
        }
    }
}
