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

namespace NodeApi
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class NodeInfoGetResult : TBase
  {
    private APIResponse _status;
    private long _now;

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

    public long Now
    {
      get
      {
        return _now;
      }
      set
      {
        __isset.now = true;
        this._now = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool status;
      public bool now;
    }

    public NodeInfoGetResult() {
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
              if (field.Type == TType.I64) {
                Now = iprot.ReadI64();
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
        TStruct struc = new TStruct("NodeInfoGetResult");
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
        if (__isset.now) {
          field.Name = "now";
          field.Type = TType.I64;
          field.ID = 2;
          oprot.WriteFieldBegin(field);
          oprot.WriteI64(Now);
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
      StringBuilder __sb = new StringBuilder("NodeInfoGetResult(");
      bool __first = true;
      if (Status != null && __isset.status) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Status: ");
        __sb.Append(Status== null ? "<null>" : Status.ToString());
      }
      if (__isset.now) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Now: ");
        __sb.Append(Now);
      }
      __sb.Append(")");
      return __sb.ToString();
    }

  }

}
