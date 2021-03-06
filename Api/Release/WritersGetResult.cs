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

namespace Release
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class WritersGetResult : TBase
  {
    private APIResponse _status;
    private int _pages;
    private List<WriterInfo> _writers;

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

    public int Pages
    {
      get
      {
        return _pages;
      }
      set
      {
        __isset.pages = true;
        this._pages = value;
      }
    }

    public List<WriterInfo> Writers
    {
      get
      {
        return _writers;
      }
      set
      {
        __isset.writers = true;
        this._writers = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool status;
      public bool pages;
      public bool writers;
    }

    public WritersGetResult() {
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
              if (field.Type == TType.I32) {
                Pages = iprot.ReadI32();
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 3:
              if (field.Type == TType.List) {
                {
                  Writers = new List<WriterInfo>();
                  TList _list54 = iprot.ReadListBegin();
                  for( int _i55 = 0; _i55 < _list54.Count; ++_i55)
                  {
                    WriterInfo _elem56;
                    _elem56 = new WriterInfo();
                    _elem56.Read(iprot);
                    Writers.Add(_elem56);
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
        TStruct struc = new TStruct("WritersGetResult");
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
        if (__isset.pages) {
          field.Name = "pages";
          field.Type = TType.I32;
          field.ID = 2;
          oprot.WriteFieldBegin(field);
          oprot.WriteI32(Pages);
          oprot.WriteFieldEnd();
        }
        if (Writers != null && __isset.writers) {
          field.Name = "writers";
          field.Type = TType.List;
          field.ID = 3;
          oprot.WriteFieldBegin(field);
          {
            oprot.WriteListBegin(new TList(TType.Struct, Writers.Count));
            foreach (WriterInfo _iter57 in Writers)
            {
              _iter57.Write(oprot);
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
      StringBuilder __sb = new StringBuilder("WritersGetResult(");
      bool __first = true;
      if (Status != null && __isset.status) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Status: ");
        __sb.Append(Status== null ? "<null>" : Status.ToString());
      }
      if (__isset.pages) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Pages: ");
        __sb.Append(Pages);
      }
      if (Writers != null && __isset.writers) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Writers: ");
        __sb.Append(Writers);
      }
      __sb.Append(")");
      return __sb.ToString();
    }

  }

}
