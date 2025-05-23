﻿using DriverService.Core.Dtos;
using MediatR;


namespace DriverService.Core.Features.Driver.Queries;

public record GetDriverLocationQuery(Guid DriverId) : IRequest<DriverLocationResponse>;
