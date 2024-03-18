﻿using StoreWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StoreWeb.DataAccess.Repository.IRepository
{
    public interface ICompanyRepository : IRepository<Company>
    {
       Task<bool> UpdateAsync(Company category);

    }
}
