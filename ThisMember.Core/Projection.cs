﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ThisMember.Core
{
  public class Projection
  {
    public Type SourceType { get; set; }

    public Type DestinationType { get; set; }

    public LambdaExpression Expression { get; set; }
  }
}