﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Routing.Models;

public class OSRMResponse
{
    public List<OSRMRoute> Routes { get; set; }
}

public class OSRMRoute
{
    public double Distance { get; set; }
    public double Duration { get; set; }
    public Geometry Geometry { get; set; }
}

public class Geometry
{
    public string Type { get; set; }
    public List<List<double>> Coordinates { get; set; }
}