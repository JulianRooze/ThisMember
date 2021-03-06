﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThisMember.Core.Interfaces;
using ThisMember.Core.Options;

namespace ThisMember.Core
{
  internal class DefaultMemberMapperConfiguration : IMemberMapperConfiguration
  {

    public MapperOptions GetOptions(IMemberMapper mapper)
    {
      return new MapperOptions();
    }

    public IMappingStrategy GetMappingStrategy(IMemberMapper mapper)
    {
      return new DefaultMappingStrategy(mapper);
    }

    public IMapGeneratorFactory GetMapGenerator(IMemberMapper mapper)
    {
      return new CompiledMapGeneratorFactory();
    }

    public IProjectionGeneratorFactory GetProjectionGenerator(IMemberMapper mapper)
    {
      return new DefaultProjectionGeneratorFactory();
    }
  }
}
