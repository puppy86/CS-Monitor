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
  public partial class SmartContractMethod : TBase
  {
    private string _name;
    private List<string> _argTypes;
    private string _returnType;

    public string Name
    {
      get
      {
        return _name;
      }
      set
      {
        __isset.name = true;
        this._name = value;
      }
    }

    public List<string> ArgTypes
    {
      get
      {
        return _argTypes;
      }
      set
      {
        __isset.argTypes = true;
        this._argTypes = value;
      }
    }

    public string ReturnType
    {
      get
      {
        return _returnType;
      }
      set
      {
        __isset.returnType = true;
        this._returnType = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool name;
      public bool argTypes;
      public bool returnType;
    }

    public SmartContractMethod() {
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
              if (field.Type == TType.String) {
                Name = iprot.ReadString();
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 2:
              if (field.Type == TType.List) {
                {
                  ArgTypes = new List<string>();
                  TList _list82 = iprot.ReadListBegin();
                  for( int _i83 = 0; _i83 < _list82.Count; ++_i83)
                  {
                    string _elem84;
                    _elem84 = iprot.ReadString();
                    ArgTypes.Add(_elem84);
                  }
                  iprot.ReadListEnd();
                }
              } else { 
                TProtocolUtil.Skip(iprot, field.Type);
              }
              break;
            case 3:
              if (field.Type == TType.String) {
                ReturnType = iprot.ReadString();
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
        TStruct struc = new TStruct("SmartContractMethod");
        oprot.WriteStructBegin(struc);
        TField field = new TField();
        if (Name != null && __isset.name) {
          field.Name = "name";
          field.Type = TType.String;
          field.ID = 1;
          oprot.WriteFieldBegin(field);
          oprot.WriteString(Name);
          oprot.WriteFieldEnd();
        }
        if (ArgTypes != null && __isset.argTypes) {
          field.Name = "argTypes";
          field.Type = TType.List;
          field.ID = 2;
          oprot.WriteFieldBegin(field);
          {
            oprot.WriteListBegin(new TList(TType.String, ArgTypes.Count));
            foreach (string _iter85 in ArgTypes)
            {
              oprot.WriteString(_iter85);
            }
            oprot.WriteListEnd();
          }
          oprot.WriteFieldEnd();
        }
        if (ReturnType != null && __isset.returnType) {
          field.Name = "returnType";
          field.Type = TType.String;
          field.ID = 3;
          oprot.WriteFieldBegin(field);
          oprot.WriteString(ReturnType);
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
      StringBuilder __sb = new StringBuilder("SmartContractMethod(");
      bool __first = true;
      if (Name != null && __isset.name) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("Name: ");
        __sb.Append(Name);
      }
      if (ArgTypes != null && __isset.argTypes) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("ArgTypes: ");
        __sb.Append(ArgTypes);
      }
      if (ReturnType != null && __isset.returnType) {
        if(!__first) { __sb.Append(", "); }
        __first = false;
        __sb.Append("ReturnType: ");
        __sb.Append(ReturnType);
      }
      __sb.Append(")");
      return __sb.ToString();
    }

  }

}
