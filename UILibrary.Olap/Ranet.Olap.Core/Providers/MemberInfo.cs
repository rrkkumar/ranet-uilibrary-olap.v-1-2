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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ranet.Olap.Core.Data;

namespace Ranet.Olap.Core.Providers
{
    /*
    public enum MemberInfoTypeEnum
    {
        Unknown,
        Regular,
        All,
        Measure,
        Formula
    }*/

    public class MemberInfo : OlapInfoBase, IProperties
    {
        public const String KEY0_PROPERTY = "KEY0";

        private static MemberInfo m_Empty;
        public static MemberInfo Empty
        {
            get
            {
                if (m_Empty == null)
                {
                    m_Empty = new MemberInfo();
                }
                return m_Empty;
            }
        }

        private long m_ChildCount = 0;
        public long ChildCount
        {
            get { return m_ChildCount; }
            set { m_ChildCount = value; }
        }

        private bool m_DrilledDown;
        public bool DrilledDown
        {
            get { return m_DrilledDown; }
            set { m_DrilledDown = value; }
        }

        /// <summary>
        /// Member is calculated
        /// </summary>
        public bool IsCalculated
        {
            get {
                return PropertiesDictionary.ContainsKey(KEY0_PROPERTY) && PropertiesDictionary[KEY0_PROPERTY] == null;
            }
        }

        private int m_LevelDepth = 0;
        public int LevelDepth
        {
            get { return m_LevelDepth; }
            set { m_LevelDepth = value; }
        }

        private String m_LevelName = String.Empty;
        public String LevelName
        {
            get { return m_LevelName; }
            set { m_LevelName = value; }
        }

        MemberInfo m_Parent = null;
        public MemberInfo Parent
        {
            get {
                return m_Parent;
            }
            set {
                m_Parent = value;
            }
        }

        public MemberInfo GetAncestor(String uniqueName)
        {
            MemberInfo parent = this.Parent;
            while(parent != null)
            {
                if (parent.UniqueName == uniqueName)
                    return parent;
                parent = parent.Parent;
            }
            return null;
        }

        public void CollectAncestors(IList<MemberInfo> list, bool ignoreSelf)
        {
            if (list == null)
                return;

            if (!ignoreSelf)
            {
                if (!list.Contains(this))
                    list.Add(this);
            }

            if (this.Parent != null)
            {
                this.Parent.CollectAncestors(list, false);
            }
        }

        public void CollectAncestors(IList<MemberInfo> list)
        {
            CollectAncestors(list, false);
        }

        public void CollectAncestors(IDictionary<String, MemberInfo> list)
        {
            if (list == null)
                return;

            if (!list.ContainsKey(this.HierarchyUniqueName))
                list.Add(this.HierarchyUniqueName, this);

            if (this.Parent != null && this.Parent != MemberInfo.Empty)
            {
                this.Parent.CollectAncestors(list);
            }
        }

        private String m_HierarchyUniqueName = String.Empty;
        public String HierarchyUniqueName
        {
            get { return m_HierarchyUniqueName; }
            set { m_HierarchyUniqueName = value; }
        }

        private String m_Custom_Rollup = String.Empty;
        public String Custom_Rollup
        {
            get { return m_Custom_Rollup; }
            set { m_Custom_Rollup = value; }
        }

        private String m_Unary_Operator = String.Empty;
        public String Unary_Operator
        {
            get { return m_Unary_Operator; }
            set { m_Unary_Operator = value; }
        }

        public bool HasCustomRollup
        {
            get {
                return !String.IsNullOrEmpty(Custom_Rollup);
            }
        }

        private bool m_ParentSameAsPrevious;
        public bool ParentSameAsPrevious
        {
            get { return m_ParentSameAsPrevious; }
            set { m_ParentSameAsPrevious = value; }
        }

/*        private MemberInfoTypeEnum m_Type = MemberInfoTypeEnum.Unknown;
        public MemberInfoTypeEnum Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }*/

        public MemberInfo()
        {
        }

        private MemberInfoCollection m_Children;
        public MemberInfoCollection Children
        {
            get
            {
                if (m_Children == null)
                {
                    m_Children = new MemberInfoCollection(this);
                }
                return m_Children;
            }
        }

        private MemberInfoCollection m_DrilledDownChildren;
        public MemberInfoCollection DrilledDownChildren
        {
            get
            {
                if (m_DrilledDownChildren == null)
                {
                    m_DrilledDownChildren = new MemberInfoCollection(this);
                }
                return m_DrilledDownChildren;
            }
        }

        /*public override int GetHashCode()
        {
            return this.UniqueName.GetHashCode();
        }*/

        /*public override bool Equals(object obj)
        {
            MemberInfo data = obj as MemberInfo;
            if (data != null)
            {
                return data.Id.Equals(this.Id) && data.Ordinal == this.Ordinal;
            }

            return base.Equals(obj);
        }*/

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Caption, this.UniqueName);
        }
        /*
        private PivotGridField(Member member, PivotGridField parent)
        {
            this.Caption = member.Caption;
            this.UniqueName = member.UniqueName;
            this.Children = new Dictionary<string, PivotGridField>();
            this.Parent = parent;
        }

        public readonly string Caption;
        public readonly string UniqueName;
        public readonly PivotGridField Parent;
        public Dictionary<string, PivotGridField> Children;

        public static PivotGridField CreateFrom(Member member, PivotGridField parent)
        {
            return new PivotGridField(member, parent);
        }
        */

        public String Key0
        {
            get
            {
                String res = String.Empty;
                if (PropertiesDictionary.ContainsKey(KEY0_PROPERTY))
                {
                    if (PropertiesDictionary[KEY0_PROPERTY] != null)
                    {
                        res = PropertiesDictionary[KEY0_PROPERTY].ToString();
                    }
                    else
                    {
                        res = "null";
                    }
                }
                return res;
            }
        }

        public String GetText(MemberVisualizationTypes type)
        {
            String res = String.Empty;

            String key0 = String.Empty;
            if (type == MemberVisualizationTypes.Key ||
                type == MemberVisualizationTypes.KeyAndCaption)
            {
                if (PropertiesDictionary.ContainsKey(KEY0_PROPERTY))
                {
                    if (PropertiesDictionary[KEY0_PROPERTY] != null)
                    {
                        key0 = PropertiesDictionary[KEY0_PROPERTY].ToString();
                    }
                    else
                    {
                        // в режиме отображения кодов вместо null нужно светить Caption,  а то получается когда в таблице несколько вычисляемых элементов у всех их светится null (ПФ)
                        // key0 = "null";
                    }
                }
            }

            // Определяем что именно нужно светить в контроле
            switch (type)
            {
                case MemberVisualizationTypes.Caption:
                    res = Caption;
                    break;
                case MemberVisualizationTypes.Key:
                    // Для элементов уровня ALL вместо ключа 0 (который никак нельзя поменять) отображаем Caption
                    if (LevelDepth == 0 && !String.IsNullOrEmpty(LevelName) && LevelName.ToLower().Contains(".[(all)]"))
                    {
                        res = Caption;
                    }
                    else
                    {
                        //Если ключ в запросе не получался, то выводим просто Caption
                        if (!String.IsNullOrEmpty(key0))
                        {
                            res = key0;
                        }
                        else
                        {
                            res = Caption;
                        }
                    }
                    break;
                case MemberVisualizationTypes.KeyAndCaption:
                    // Для элементов уровня ALL вместо ключа 0 (который никак нельзя поменять) отображаем Caption
                    if (LevelDepth == 0 && !String.IsNullOrEmpty(LevelName) && LevelName.ToLower().Contains(".[(all)]"))
                    {
                        res = Caption;
                    }
                    else
                    {
                        //Если ключ в запросе не получался, то выводим просто Caption
                        if (!String.IsNullOrEmpty(key0))
                        {
                            res = key0 + " " + Caption;
                        }
                        else
                        {
                            res = Caption;
                        }
                    }
                    break;
                case MemberVisualizationTypes.UniqueName:
                    res = UniqueName;
                    break;
                default:
                    res = Caption;
                    break;
            }
            return res;
        }

        #region IProperties Members

        Dictionary<string, object> m_PropertiesDictionary = null;
        public Dictionary<string, object> PropertiesDictionary
        {
            get {
                if (m_PropertiesDictionary == null)
                {
                    m_PropertiesDictionary = new Dictionary<string, object>();
                    m_PropertiesDictionary.Add("Caption", this.Caption);
                    m_PropertiesDictionary.Add("Name", this.Name);
                    m_PropertiesDictionary.Add("UniqueName", this.UniqueName);
                    m_PropertiesDictionary.Add("ChildCount", this.ChildCount);
                    m_PropertiesDictionary.Add("DrilledDown", this.DrilledDown);
                    m_PropertiesDictionary.Add("Description", this.Description);
                    //m_PropertiesDictionary.Add("Custom_Rollup", this.Custom_Rollup);
                    //m_PropertiesDictionary.Add("Unary_Operator", this.Unary_Operator);
                    m_PropertiesDictionary.Add("HierarchyUniqueName", this.HierarchyUniqueName);
                    m_PropertiesDictionary.Add("LevelDepth", this.LevelDepth);
                    m_PropertiesDictionary.Add("LevelName", this.LevelName);
                    m_PropertiesDictionary.Add("ParentSameAsPrevious", this.ParentSameAsPrevious);
                }
                return m_PropertiesDictionary;
            }
        }

        #endregion
    }
}
