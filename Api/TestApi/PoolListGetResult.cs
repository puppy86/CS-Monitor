/**
 * Autogenerated by Thrift Compiler (0.11.0)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace TestApi
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class PoolListGetResult : TBase
  {
    private APIResponse _status;
    private bool _result;
    private List<Pool> _pools;

    public APIResponse Status
    {
      get
      {
        return _status;
      }
      set
      {
        __isset.status = true;
        this._status = value;
      }
    }

    public bool Result
    {
      get
      {
        return _result;
      }
      set
      {
        __isset.result = true;
        this._result = value;
      }
    }

    public List<Pool> Pools
    {
      get
      {
        return _pools;
      }
      set
      {
        __isset.pools = true;
        this._pools = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool status;
      public bool result;
      public bool pools;
    }

    public PoolListGetResult() {
    }

    public void Read (TProtocol iprot)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        TField field;
        iprot.ReadStructBegin();
        while (true)
        {
          field = iprot.ReadFieldBegin();
          if (field.Type == TType.Stop) { 
            break;
          }
          switch (field.ID)
          {
            case 1:
              if (field.Type == TType.Struct) {
                Status = new APIResponse();
                Status.Read(iprot);
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 2:
              if (field.Type == TType.Bool) {
                Result = iprot.ReadBool();
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 3:
              if (field.Type == TType.List) {
                {
                  Pools = new List<Pool>();
                  TList _list26 = iprot.ReadListBegin();
                  for( int _i27 = 0; _i27 < _list26.Count; ++_i27)
                  {
                    Pool _elem28;
                    _elem28 = new Pool();
                    _elem28.Read(iprot);
                    Pools.Add(_elem28);
                  }
                  iprot.ReadListEnd();
                }
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            default: 
              TProtocolUtil.Skip(iprot, field.Type);
              break;
          }
          iprot.ReadFieldEnd();
        }
        iprot.ReadStructEnd();
      }
      finally
      {
        iprot.DecrementRecursionDepth();
      }
    }

    public void Write(TProtocol oprot) {
      oprot.IncrementRecursionDepth();
      try
      {
        TStruct struc = new TStruct("PoolListGetResult");
        oprot.WriteStructBegin(struc);
        TField field = new TField();
        if (Status != null && __isset.status) {
          field.Name = "status";
          field.Type = TType.Struct;
          field.ID = 1;
          oprot.WriteFieldBegin(field);
          Status.Write(oprot);
          oprot.WriteFieldEnd();
        }
        if (__isset.result) {
          field.Name = "result";
          field.Type = TType.Bool;
          field.ID = 2;
          oprot.WriteFieldBegin(field);
          oprot.WriteBool(Result);
          oprot.WriteFieldEnd();
        }
        if (Pools != null && __isset.pools) {
          field.Name = "pools";
          field.Type = TType.List;
          field.ID = 3;
          oprot.WriteFieldBegin(field);
          {
            oprot.WriteListBegin(new TList(TType.Struct, Pools.Count));
            foreach (Pool _iter29 in Pools)
            {
              _iter29.Write(oprot);
            }
            oprot.WriteListEnd();
          }
          oprot.WriteFieldEnd();
        }
        oprot.WriteFieldStop();
        oprot.WriteStructEnd();
      }
      finally
      {
        oprot.DecrementRecursionDepth();
      }
    }

    public override string ToString() {
      StringBuilder __sb = new StringBuilder("PoolListGetResult(");
      bool __first = true;
      if (Status != null && __isset.status) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Status: ");
        __sb.Append(Status== null ? "<null>" : Status.ToString());
      }
      if (__isset.result) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Result: ");
        __sb.Append(Result);
      }
      if (Pools != null && __isset.pools) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Pools: ");
        __sb.Append(Pools);
      }
      __sb.Append(")");
      return __sb.ToString();
    }

  }

}
