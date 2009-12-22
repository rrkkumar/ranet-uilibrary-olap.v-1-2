﻿/*   
    Copyright (C) 2009 Galaktika Corporation ZAO

    This file is a part of Ranet.UILibrary.Olap
 
    Ranet.UILibrary.Olap is a free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
      
    You should have received a copy of the GNU General Public License
    along with Ranet.UILibrary.Olap.  If not, see <http://www.gnu.org/licenses/>.
  
    If GPL v.3 is not suitable for your products or company,
    Galaktika Corp provides Ranet.UILibrary.Olap under a flexible commercial license
    designed to meet your specific usage and distribution requirements.
    If you have already obtained a commercial license from Galaktika Corp,
    you can use this file under those license terms.
*/

using System;
using System.Net;
using System.Collections.Generic;

namespace Ranet.Olap.Core.Providers.ClientServer
{
    public class UpdateCubeArgs : OlapActionBase
    {
        public UpdateCubeArgs()
        {
            ActionType = OlapActionTypes.UpdateCube;
        }
        
        public String PivotID = String.Empty;
        public string ConnectionString = string.Empty;
        public string CubeName = string.Empty;
        //public string Script = string.Empty;

        public List<UpdateEntry> Entries = new List<UpdateEntry>();
    }

    /// <summary>
    /// Класс, задающий правило обновления для ячейки
    /// </summary>
    public class UpdateEntry
    {
        public UpdateEntry()
        {
        }

        public UpdateEntry(string newValue)
        {
            this.NewValue = newValue;
        }

        /// <summary>
        /// Координата ячейки
        /// </summary>
        public readonly List<ShortMemberInfo> Tuple = new List<ShortMemberInfo>();

        /// <summary>
        /// Новое значение ячейки
        /// </summary>
        public string NewValue = string.Empty;

        /// <summary>
        /// Старое значение ячейки
        /// </summary>
        public string OldValue = string.Empty;

    }
}