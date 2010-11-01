#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using Gentle.Framework;
using TvLibrary.Log;

namespace TvDatabase
{
  /// <summary>
  /// Instances of this class represent the properties and methods of a row in the table <b>GroupMap</b>.
  /// </summary>
  [TableName("RadioGroupMap")]
  public class RadioGroupMap : Persistent
  {
    #region Members

    private bool isChanged;
    [TableColumn("idMap", NotNull = true), PrimaryKey(AutoGenerated = true)] private int idMap;
    [TableColumn("idGroup", NotNull = true), ForeignKey("ChannelGroup", "idGroup")] private int idGroup;
    [TableColumn("idChannel", NotNull = true), ForeignKey("Channel", "idChannel")] private int idChannel;
    [TableColumn("SortOrder", NotNull = true)] private int sortOrder;

    #endregion

    #region Constructors

    /// <summary> 
    /// Create a new object by specifying all fields (except the auto-generated primary key field). 
    /// </summary> 
    public RadioGroupMap(int idGroup, int idChannel, int sortOrder)
    {
      isChanged = true;
      this.idGroup = idGroup;
      this.idChannel = idChannel;
      this.sortOrder = sortOrder;
    }

    /// <summary> 
    /// Create an object from an existing row of data. This will be used by Gentle to 
    /// construct objects from retrieved rows. 
    /// </summary> 
    public RadioGroupMap(int idMap, int idGroup, int idChannel, int sortOrder)
    {
      this.idMap = idMap;
      this.idGroup = idGroup;
      this.idChannel = idChannel;
      this.sortOrder = sortOrder;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Indicates whether the entity is changed and requires saving or not.
    /// </summary>
    public bool IsChanged
    {
      get { return isChanged; }
    }

    /// <summary>
    /// Property relating to database column idMap
    /// </summary>
    public int IdMap
    {
      get { return idMap; }
    }

    /// <summary>
    /// Property relating to database column idGroup
    /// </summary>
    public int IdGroup
    {
      get { return idGroup; }
      set
      {
        isChanged |= idGroup != value;
        idGroup = value;
      }
    }

    /// <summary>
    /// Property relating to database column idChannel
    /// </summary>
    public int IdChannel
    {
      get { return idChannel; }
      set
      {
        isChanged |= idChannel != value;
        idChannel = value;
      }
    }

    /// <summary>
    /// Property relating to database column SortOrder
    /// </summary>
    public int SortOrder
    {
      get { return sortOrder; }
      set
      {
        isChanged |= sortOrder != value;
        sortOrder = value;
      }
    }

    #endregion

    #region Storage and Retrieval

    /// <summary>
    /// Static method to retrieve all instances that are stored in the database in one call
    /// </summary>
    public static IList<RadioGroupMap> ListAll()
    {
      return Broker.RetrieveList<RadioGroupMap>();
    }

    /// <summary>
    /// Retrieves an entity given it's id.
    /// </summary>
    public static RadioGroupMap Retrieve(int id)
    {
      // Return null if id is smaller than seed and/or increment for autokey
      if (id < 1)
      {
        return null;
      }
      Key key = new Key(typeof (GroupMap), true, "idMap", id);
      return Broker.RetrieveInstance<RadioGroupMap>(key);
    }

    /// <summary>
    /// Retrieves an entity given it's id, using Gentle.Framework.Key class.
    /// This allows retrieval based on multi-column keys.
    /// </summary>
    public static RadioGroupMap Retrieve(Key key)
    {
      return Broker.RetrieveInstance<RadioGroupMap>(key);
    }

    /// <summary>
    /// Persists the entity if it was never persisted or was changed.
    /// </summary>
    public override void Persist()
    {
      if (IsChanged || !IsPersisted)
      {
        try
        {
          base.Persist();
        }
        catch (Exception ex)
        {
          Log.Error("Exception in RadioGroupMap.Persist() with Message {0}", ex.Message);
          return;
        }
        isChanged = false;
      }
    }

    #endregion

    #region Relations

    /// <summary>
    ///
    /// </summary>
    public Channel ReferencedChannel()
    {
      return Channel.Retrieve(IdChannel);
    }

    /// <summary>
    ///
    /// </summary>
    public RadioChannelGroup ReferencedRadioChannelGroup()
    {
      return RadioChannelGroup.Retrieve(IdGroup);
    }

    #endregion
  }
}