﻿using OpenTK.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CgfConverter
{
    public partial class CryEngine
    {
        // MatType for type 800, 0x1 is material library, 0x12 is child, 0x10 is solo material

        /// <summary>
        /// CryEngine cgf/cga/skin file handler
        /// </summary>
        public class Model
        {
            public static Model FromFile(String fileName)
            {
                Model buffer = new Model();
                buffer.Load(fileName);
                return buffer;
            }

            public Model()
            {
                this.ChunkMap = new Dictionary<UInt32, Model.Chunk> { };
                this.ChunkHeaders = new List<ChunkHeader>();
                this.Headers = new ChunkTable();                    
            }

            //private Dictionary<UInt32, Chunk> _chunksByID;
            //public Dictionary<UInt32, Chunk> ChunksByID
            //{
            //    get
            //    {
            //        if (this._chunksByID == null)
            //            this._MapChunks();

            //        return this._chunksByID;
            //    }
            //}

            //private Dictionary<String, ChunkNode> _chunksByName;
            //public Dictionary<String, ChunkNode> ChunksByName
            //{
            //    get
            //    {
            //        if (this._chunksByName == null)
            //            this._MapChunks();

            //        return this._chunksByName;
            //    }
            //}

            //private void _MapChunks()
            //{
            //    HashSet<String> watchlist = new HashSet<String>
            //    {
            //        "LG_Hatch_Front_Outboard_Left",
            //        "LG_Skid_Front_Right",
            //        "LG_Arm_Front_Right"
            //    };

            //    this._chunksByID = new Dictionary<UInt32, Chunk>();
            //    this._chunksByName = new Dictionary<String, ChunkNode>();

            //    foreach (var chunk in this.CgfChunks)
            //    {
            //        this._chunksByID[chunk.id] = chunk;
            //        if (chunk.ChunkType == ChunkType.Node)
            //        {
            //            var node = (chunk as ChunkNode);

            //            // if (this._chunksByName.ContainsKey(node.Name))
            //            //     Console.WriteLine("Overwriting Part {0} > {1} with {2} > {3}", this._chunksByName[node.Name].ParentNodeName, this._chunksByName[node.Name].Name, node.ParentNodeName, node.Name);
            //            // else
            //            // {

            //            // LG_Hatch_Front_Outboard_Left
            //            // LG_Skid_Front_Right
                        
            //            // if (watchlist.Contains(node.Name))
            //            // {
            //            //     node.WriteChunk();
            //            // }

            //            if (this._chunksByName.ContainsKey(node.Name))
            //            {
            //                var oldNode = this._chunksByName[node.Name];

            //                if ((node.NodeFile != this.Args.InputFiles.First().FullName) && (node.ParentNodeName != oldNode.ParentNodeName))
            //                {
            //                    Console.WriteLine("Discarding mFile Parent for {2}.{1}", node.ParentNodeName, node.Name, oldNode.ParentNodeName);
            //                    node.ParentNode = oldNode.ParentNode;
            //                    this._chunksByName[node.Name] = node;
            //                }
            //                else
            //                {

            //                }
            //            }
            //            else
            //            {
            //                this._chunksByName[node.Name] = node;
            //            }
            //        }
            //    }
            //}
            // public List<Chunk> CgfChunks = new List<Chunk>();   //  I don't think we want this.  Dictionary is better because of ID

            #region Legacy

            // Header, ChunkTable and Chunks are what are in a file.  1 header, 1 table, and a chunk for each entry in the table.
            private static Int32 FILE_VERSION;
            private static UInt32 NUM_CHUNKS;          // number of chunks in the chunk table

            public FileHeader CgfHeader { get; private set; }
            /// <summary>
            /// CgfChunkTable contains a list of all the Chunks.
            /// </summary>
            public ChunkTable Headers { get; private set; }
            public Dictionary<UInt32, Model.Chunk> ChunkMap { get; private set; }
            public ChunkNode RootNode { get; private set; }
            public List<ChunkHeader> ChunkHeaders {get; private set; }

            /// <summary>
            /// Load a cgf/cga/skin file
            /// </summary>
            /// <param name="fileName"></param>
            public void Load(String fileName)
            {
                FileInfo inputFile = new FileInfo(fileName);

                if (!inputFile.Exists)
                    throw new FileNotFoundException();

                // Open the file for reading.
                BinaryReader cgfReader = new BinaryReader(File.Open(fileName, FileMode.Open));
                // Get the header.  This isn't essential for .cgam files, but we need this info to find the version and offset to the chunk table
                this.CgfHeader = new FileHeader();                       // Gets the header of the file (3-5 objects dep on version)
                this.CgfHeader.GetHeader(cgfReader);
                NUM_CHUNKS = this.CgfHeader.NumChunks;
                this.Headers.GetChunkTable(cgfReader, CgfHeader.ChunkTableOffset);
                this.Headers.WriteChunk();

                foreach (ChunkHeader chkHdr in this.Headers.Items)
                {
                    //Console.WriteLine("Processing {0}", ChkHdr.type);
                    switch (chkHdr.Type)
                    {
                        case ChunkTypeEnum.SourceInfo:
                            ChunkSourceInfo chkSrcInfo = new ChunkSourceInfo(this);
                            chkSrcInfo.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkSrcInfo;
                            break;
                        case ChunkTypeEnum.Timing:
                            // Timing chunks don't have IDs for some reason.
                            ChunkTimingFormat chkTiming = new ChunkTimingFormat(this);
                            chkTiming.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkTiming;
                            break;
                        case ChunkTypeEnum.ExportFlags:
                            ChunkExportFlags chkExportFlag = new ChunkExportFlags(this);
                            chkExportFlag.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkExportFlag;
                            break;
                        case ChunkTypeEnum.Mtl:
                            //Console.WriteLine("Mtl Chunk here");  // Obsolete.  Not used?
                            break;
                        case ChunkTypeEnum.MtlName:
                            ChunkMtlName chkMtlName = new ChunkMtlName(this);
                            chkMtlName.Version = chkHdr.Version;
                            chkMtlName.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkMtlName;
                            break;
                        case ChunkTypeEnum.DataStream:
                            ChunkDataStream chkDataStream = new ChunkDataStream(this);
                            chkDataStream.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkDataStream;
                            break;
                        case ChunkTypeEnum.Mesh:
                            ChunkMesh chkMesh = new ChunkMesh(this);
                            chkMesh.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkMesh;
                            break;
                        case ChunkTypeEnum.MeshSubsets:
                            ChunkMeshSubsets chkMeshSubsets = new ChunkMeshSubsets(this);
                            chkMeshSubsets.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkMeshSubsets;
                            break;
                        case ChunkTypeEnum.Node:
                            ChunkNode chkNode = new ChunkNode(this);
                            chkNode.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkNode;

                            // TODO: Change this to detect node with NULL or 0xFFFFFFFF parent ID
                            // Assume first node read is root node
                            if (this.RootNode == null)
                            {
                                this.RootNode = chkNode;
                            }
                            break;
                        case ChunkTypeEnum.CompiledBones:
                            ChunkCompiledBones chkCompiledBones = new ChunkCompiledBones(this);
                            chkCompiledBones.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkCompiledBones;
                            break;
                        case ChunkTypeEnum.Helper:
                            ChunkHelper chkHelper = new ChunkHelper(this);
                            chkHelper.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkHelper;
                            break;
                        case ChunkTypeEnum.Controller:
                            ChunkController chkController = new ChunkController(this);
                            chkController.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkController;
                            break;
                        case ChunkTypeEnum.SceneProps:
                            ChunkSceneProp chkSceneProp = new ChunkSceneProp(this);
                            chkSceneProp.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkSceneProp;
                            break;
                        case ChunkTypeEnum.CompiledPhysicalProxies:
                            ChunkCompiledPhysicalProxies chkCompiledPhysicalProxy = new ChunkCompiledPhysicalProxies(this);
                            chkCompiledPhysicalProxy.ReadChunk(cgfReader, chkHdr);
                            this.ChunkMap[chkHdr.ID] = chkCompiledPhysicalProxy;
                            break;
                        default:
                            // If we hit this point, it's an unimplemented chunk and needs to be added.
                            Console.WriteLine("Chunk type found that didn't match known versions: {0}", chkHdr.Type);
                            break;
                    }
                }
            }

            public void WriteTransform(Vector3 transform)
            {
                Console.WriteLine("Transform:");
                Console.WriteLine("{0}    {1}    {2}", transform.x, transform.y, transform.z);
                Console.WriteLine();
            }

            #region DataTypes

            public class FileHeader
            {
                public Char[] FileSignature; // The CGF file signature.  CryTek for 3.5, CrChF for 3.6
                public UInt32 FileType; // The CGF file type (geometry or animation)  3.5 only
                public UInt32 ChunkVersion; // The version of the chunk table 3.5 only
                public Int32 ChunkTableOffset; // Position of the chunk table in the CGF file
                /// <summary>
                /// 3.6 Only - Number of chunks in the Chunk Table
                /// </summary>
                public UInt32 NumChunks { get; private set; }
                //public Int32 FileVersion;         // 0 will be 3.4 and older, 1 will be 3.6 and newer.  THIS WILL CHANGE
                // methods
                public void GetHeader(BinaryReader b)  //constructor with 1 arg
                {
                    //Header cgfHeader = new Header();
                    // populate the Header objects
                    FileSignature = new Char[8];
                    FileSignature = b.ReadChars(8);
                    String s = new string(FileSignature);
                    Console.Write("fileSignature is {0}, ", s);
                    if (s.ToLower().Contains("crytek"))
                    {
                        Console.WriteLine("Version 3.4 or earlier");
                        FileType = b.ReadUInt32();
                        ChunkVersion = b.ReadUInt32();
                        ChunkTableOffset = b.ReadInt32();  // location of the chunk table
                        FILE_VERSION = 0;                     // File version 0 is Cryengine 3.4 and older
                    }
                    else
                    {
                        Console.WriteLine("Crytek Version 3.6 or newer");
                        this.NumChunks = b.ReadUInt32();  // number of Chunks in the chunk table
                        ChunkTableOffset = b.ReadInt32(); // location of the chunk table
                        FILE_VERSION = 1;                    // File version 1 is Cryengine 3.6 and newer 
                    }
                    // WriteChunk();
                    return;
                }
                public void WriteChunk()  // output header to console for testing
                {
                    String tmpFileSig;
                    tmpFileSig = new string(FileSignature);
                    Console.WriteLine("*** HEADER ***");
                    Console.WriteLine("    Header Filesignature: {0}", tmpFileSig);
                    if (tmpFileSig.ToLower().Contains("crytek"))
                    {
                        Console.WriteLine("    FileType:            {0:X}", FileType);
                        Console.WriteLine("    ChunkVersion:        {0:X}", ChunkVersion);
                        Console.WriteLine("    ChunkTableOffset:    {0:X}", ChunkTableOffset);
                    }
                    else
                    {
                        Console.WriteLine("    NumChunks:           {0:X}", NUM_CHUNKS);
                        Console.WriteLine("    ChunktableOffset:    {0:X}", ChunkTableOffset);
                    }

                    Console.WriteLine("*** END HEADER ***");
                    return;
                }
            }

            public class ChunkTable  // reads the chunk table into a list of ChunkHeaders
            {
                public List<ChunkHeader> Items = new List<ChunkHeader>();

                // methods
                public void GetChunkTable(BinaryReader b, Int32 f)
                {
                    // need to seek to the start of the table here.  foffset points to the start of the table
                    b.BaseStream.Seek(f, SeekOrigin.Begin);

                    if (Model.FILE_VERSION == 0)           // old 3.4 format
                    {
                        NUM_CHUNKS = b.ReadUInt32();  // number of Chunks in the table.
                    }

                    for (Int32 i = 0; i < NUM_CHUNKS; i++)
                    {
                        ChunkHeader tempChkHdr = new ChunkHeader();
                        tempChkHdr.ReadChunk(b);
                        Items.Add(tempChkHdr);
                    }
                }
                public void WriteChunk()
                {
                    Console.WriteLine("*** Chunk Header Table***");
                    Console.WriteLine("Chunk Type              Version   ID        Size      Offset    ");
                    foreach (ChunkHeader chkHdr in Items)
                    {
                        Console.WriteLine("{0,-24}{1,-10:X}{2,-10:X}{3,-10:X}{4,-10:X}", chkHdr.Type, chkHdr.Version, chkHdr.ID, chkHdr.Size, chkHdr.Offset);
                    }
                }
            }

            public class ChunkHeader
            {
                public ChunkTypeEnum Type { get; private set; }
                public UInt32 Version { get; private set; }
                public UInt32 Offset { get; private set; }
                public UInt32 ID { get; private set; }
                public UInt32 Size { get; private set; }

                public void ReadChunk(BinaryReader b)
                {
                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 headerType = b.ReadUInt32(); // read the value, then parse it
                        this.Type = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), headerType);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();
                        this.ID = b.ReadUInt32();  // This is the chunk ID (except timing)
                        // hack to fix the timing chunk ID, since we don't want it to conflict.  Add 0xFFFF0000 to it.
                        if ((this.Type == ChunkTypeEnum.Timing) || ((UInt32)this.Type == 0x100E))
                        {
                            this.ID = this.ID + 0xFFFF0000;
                        }
                        this.Size = b.ReadUInt32();
                    }
                    else if (Model.FILE_VERSION == 1) // Newer 3.7+ format.  Only know of Star Citizen using this for now.
                    {
                        UInt16 headerType = b.ReadUInt16();
                        switch (headerType)
                        {
                            case 0x1000: this.Type = ChunkTypeEnum.Mesh; break;
                            case 0x1001: this.Type = ChunkTypeEnum.Helper; break;
                            case 0x1002: this.Type = ChunkTypeEnum.VertAnim; break;
                            case 0x1003: this.Type = ChunkTypeEnum.BoneAnim; break;
                            case 0x1004: this.Type = ChunkTypeEnum.GeomNameList; break;
                            case 0x1005: this.Type = ChunkTypeEnum.BoneNameList; break;
                            case 0x1006: this.Type = ChunkTypeEnum.MtlList; break;
                            case 0x1007: this.Type = ChunkTypeEnum.MRM; break;
                            case 0x1008: this.Type = ChunkTypeEnum.SceneProps; break;
                            case 0x1009: this.Type = ChunkTypeEnum.Light; break;
                            case 0x100A: this.Type = ChunkTypeEnum.PatchMesh; break;
                            case 0x100B: this.Type = ChunkTypeEnum.Node; break;
                            case 0x100C: this.Type = ChunkTypeEnum.Mtl; break;
                            case 0x100D: this.Type = ChunkTypeEnum.Controller; break;
                            case 0x100E: this.Type = ChunkTypeEnum.Timing; break;
                            case 0x100F: this.Type = ChunkTypeEnum.BoneMesh; break;
                            case 0x1010: this.Type = ChunkTypeEnum.BoneLightBinding; break;
                            case 0x1011: this.Type = ChunkTypeEnum.MeshMorphTarget; break;
                            case 0x1012: this.Type = ChunkTypeEnum.BoneInitialPos; break;
                            case 0x1013: this.Type = ChunkTypeEnum.SourceInfo; break;
                            case 0x1014: this.Type = ChunkTypeEnum.MtlName; break;
                            case 0x1015: this.Type = ChunkTypeEnum.ExportFlags; break;
                            case 0x1016: this.Type = ChunkTypeEnum.DataStream; break;
                            case 0x1017: this.Type = ChunkTypeEnum.MeshSubsets; break;
                            case 0x1018: this.Type = ChunkTypeEnum.MeshPhysicsData; break;
                            default:
                                Console.WriteLine("Unknown Chunk Type found {0:X}.  Skipping...", headerType);
                                break;
                        }
                        //this.type36 = (ChunkType36)Enum.ToObject(typeof(ChunkType36), this);
                        //Console.WriteLine("headerType: '{0}'", this.type);
                        this.Version = (uint)b.ReadUInt16();
                        this.ID = b.ReadUInt32();  // This is the reference number to identify the mesh/datastream
                        this.Size = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();
                    }
                }

                // methods
                public void WriteChunk()  // write the Chunk Header Table to the console.  For testing.
                {
                    Console.WriteLine("*** CHUNK HEADER ***");
                    Console.WriteLine("    ChunkType: {0}", Type);
                    Console.WriteLine("    ChunkVersion: {0:X}", Version);
                    Console.WriteLine("    offset: {0:X}", Offset);
                    Console.WriteLine("    ID: {0:X}", ID);
                    Console.WriteLine("*** END CHUNK HEADER ***");
                }
            }

            public abstract class Chunk
            {
                public Chunk(CryEngine.Model model)
                {
                    this._model = model;
                }

                internal Model _model;

                public UInt32 Offset { get; internal set; }
                /// <summary>
                /// The Type of the Chunk
                /// </summary>
                public ChunkTypeEnum ChunkType;
                /// <summary>
                /// The Version of this Chunk
                /// </summary>
                public UInt32 Version;
                /// <summary>
                /// The ID of this Chunk
                /// </summary>
                public UInt32 ID;
                /// <summary>
                /// The Size of this Chunk (in Bytes)
                /// </summary>
                public UInt32 Size;

                public virtual void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    this.ChunkType = hdr.Type;
                    this.Version = hdr.Version;
                    this.Offset = hdr.Offset;
                    this.ID = hdr.ID;
                    this.Size = hdr.Size;

                    b.BaseStream.Seek(hdr.Offset, 0);
                }

                public virtual void WriteChunk()
                {
                    Console.WriteLine("*** CHUNK ***");
                    Console.WriteLine("    ChunkType: {0}", this.ChunkType);
                    Console.WriteLine("    ChunkVersion: {0:X}", this.Version);
                    Console.WriteLine("    Offset: {0:X}", this.Offset);
                    Console.WriteLine("    ID: {0:X}", this.ID);
                    Console.WriteLine("    Size: {0:X}", this.Size);
                    Console.WriteLine("*** END CHUNK ***");
                }
            }

            public class ChunkHelper : Chunk        // cccc0001:  Helper chunk.  This is the top level, then nodes, then mesh, then mesh subsets
            {
                public String Name;
                public HelperTypeEnum HelperType;
                public Vector3 Pos;
                public Matrix44 Transform;

                public ChunkHelper(Model model) : base(model) { }

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), b.ReadUInt32());
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();
                        this.ID = b.ReadUInt32();
                    }

                    this.HelperType = (HelperTypeEnum)Enum.ToObject(typeof(HelperTypeEnum), b.ReadUInt32());
                    if (this.Version == 0x744)  // only has the Position.
                    {
                        this.Pos.x = b.ReadSingle();
                        this.Pos.y = b.ReadSingle();
                        this.Pos.z = b.ReadSingle();
                    }
                    else if (this.Version == 0x362)   // will probably never see these.
                    {
                        Char[] tmpName = new Char[64];
                        tmpName = b.ReadChars(64);
                        Int32 stringLength = 0;
                        for (Int32 i = 0, j = tmpName.Length; i < j; i++)
                        {
                            if (tmpName[i] == 0)
                            {
                                stringLength = i;
                                break;
                            }
                        }
                        this.Name = new string(tmpName, 0, stringLength);
                        this.HelperType = (HelperTypeEnum)Enum.ToObject(typeof(HelperTypeEnum), b.ReadUInt32());
                        this.Pos.x = b.ReadSingle();
                        this.Pos.y = b.ReadSingle();
                        this.Pos.z = b.ReadSingle();
                    }
                }

                public override void WriteChunk()
                {
                    Console.WriteLine("*** START Helper Chunk ***");
                    Console.WriteLine("    ChunkType:   {0}", ChunkType);
                    Console.WriteLine("    Version:     {0:X}", Version);
                    Console.WriteLine("    ID:          {0:X}", ID);
                    Console.WriteLine("    HelperType:  {0}", HelperType);
                    Console.WriteLine("    Position:    {0}, {1}, {2}", Pos.x, Pos.y, Pos.z);
                    Console.WriteLine("*** END Helper Chunk ***");
                }
            }

            public class ChunkCompiledBones : Chunk     //  0xACDC0000:  Bones info
            {
                public UInt32[] Reserved;             // 8 reserved bytes
                public String RootBoneID;          // Controller ID?  Name?  Not sure yet.
                public CompiledBone RootBone;       // First bone in the data structure.  Usually Bip01
                public UInt32 NumBones;               // Number of bones in the chunk
                // Bone info
                //public Dictionary<UInt32, CompiledBone> BoneDictionary = new Dictionary<UInt32, CompiledBone>();
                public Dictionary<String, CompiledBone> BoneDictionary = new Dictionary<String, CompiledBone>();  // Name and CompiledBone object

                public ChunkCompiledBones(Model model) : base(model) { }

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpNodeChunk = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpNodeChunk);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();
                        this.ID = b.ReadUInt32();
                    }
                    this.Reserved = new uint[8];
                    for (Int32 i = 0; i < 8; i++)
                    {
                        this.Reserved[i] = b.ReadUInt32();
                    }

                    //  Read the first bone with ReadCompiledBone, then recursively grab all the children for each bone you find.
                    //  Each bone structure is 584 bytes, so will need to seek childOffset * 584 each time, and go back.

                    this.GetCompiledBones(b, "isRoot");                        // Start reading at the root bone
                }

                public void GetCompiledBones(BinaryReader b, String parent)        // Recursive call to read the bone at the current seek, and all children.
                {
                    // Start reading all the properties of this bone.
                    CompiledBone tempBone = new CompiledBone();
                    // Console.WriteLine("** Current offset {0:X}", b.BaseStream.Position);
                    tempBone.offset = b.BaseStream.Position;
                    tempBone.ReadCompiledBone(b);
                    tempBone.parentID = parent;
                    //tempBone.WriteCompiledBone();
                    tempBone.childNames = new String[tempBone.numChildren];
                    this.BoneDictionary[tempBone.boneName] = tempBone;         // Add this bone to the dictionary.

                    for (Int32 i = 0; i < tempBone.numChildren; i++)
                    {
                        // If child offset is 1, then we're at the right position anyway.  If it's 2, you want to 584 bytes.  3 is (584*2)...
                        // Move to the offset of child.  If there are no children, we shouldn't move at all.
                        b.BaseStream.Seek(tempBone.offset + 584 * tempBone.offsetChild + (i * 584), 0);
                        GetCompiledBones(b, tempBone.boneName);
                    }
                    // Need to set the seek position back to the parent at this point?  Can use parent offset * 584...  Parent offset is a neg number
                    //Console.WriteLine("Parent offset: {0}", tempBone.offsetParent);
                }

                public override void WriteChunk()
                {
                    Console.WriteLine("*** START CompiledBone Chunk ***");
                    Console.WriteLine("    ChunkType:           {0}", ChunkType);
                    Console.WriteLine("    Node ID:             {0:X}", ID);
                }
            }

            public class ChunkCompiledPhysicalProxies : Chunk        // 0xACDC0003:  Hit boxes?
            {
                // Properties.  VERY similar to datastream, since it's essential vertex info.
                public UInt32 Flags2;
                public UInt32 NumBones; // Number of data entries
                public UInt32 BytesPerElement; // Bytes per data entry
                //public UInt32 Reserved1;
                //public UInt32 Reserved2;
                public HitBox[] HitBoxes;

                public ChunkCompiledPhysicalProxies(Model model) : base(model) { }

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpNodeChunk = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpNodeChunk);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();
                        this.ID = b.ReadUInt32();
                        // Console.WriteLine("Chunk ID is {0:X}", id);
                    }

                    this.NumBones = b.ReadUInt32(); // number of Bones in this chunk.
                    // Console.WriteLine("Number of bones (hitboxes): {0}", NumBones);
                    this.HitBoxes = new HitBox[NumBones];    // now have an array of hitboxes
                    for (Int32 i = 0; i < NumBones; i++)
                    {
                        // Start populating the hitbox array
                        this.HitBoxes[i].ID = b.ReadUInt32();
                        this.HitBoxes[i].NumVertices = b.ReadUInt32();
                        this.HitBoxes[i].NumIndices = b.ReadUInt32();
                        this.HitBoxes[i].Unknown2 = b.ReadUInt32();      // Probably a fill of some sort?
                        this.HitBoxes[i].Vertices = new Vector3[HitBoxes[i].NumVertices];
                        this.HitBoxes[i].Indices = new UInt16[HitBoxes[i].NumIndices];

                        //Console.WriteLine("Hitbox {0}, {1:X} Vertices and {2:X} Indices", i, HitBoxes[i].NumVertices, HitBoxes[i].NumIndices);
                        for (Int32 j = 0; j < HitBoxes[i].NumVertices; j++)
                        {
                            HitBoxes[i].Vertices[j].x = b.ReadSingle();
                            HitBoxes[i].Vertices[j].y = b.ReadSingle();
                            HitBoxes[i].Vertices[j].z = b.ReadSingle();
                            // Console.WriteLine("{0} {1} {2}",HitBoxes[i].Vertices[j].x,HitBoxes[i].Vertices[j].y,HitBoxes[i].Vertices[j].z);
                        }
                        // Read the indices
                        for (Int32 j = 0; j < HitBoxes[i].NumIndices; j++)
                        {
                            HitBoxes[i].Indices[j] = b.ReadUInt16();
                            //Console.WriteLine("Indices: {0}", HitBoxes[i].Indices[j]);
                        }
                        // Console.WriteLine("Index 0 is {0}, Index 9 is {1}", HitBoxes[i].Indices[0],HitBoxes[i].Indices[9]);
                        // read the crap at the end so we can move on.
                        for (Int32 j = 0; j < HitBoxes[i].Unknown2 / 2; j++)
                        {
                            b.ReadUInt16();
                        }
                        // HitBoxes[i].WriteHitBox();
                    }

                }
                public override void WriteChunk()
                {
                    base.WriteChunk();
                }
            }

            public class ChunkNode : Chunk          // cccc000b:   Node
            {
                #region Chunk Properties

                /// <summary>
                /// Chunk Name (String[64])
                /// </summary>
                public String Name { get; internal set; }
                /// <summary>
                /// Mesh or Helper Object ID
                /// </summary>
                public UInt32 Object { get; internal set; }
                /// <summary>
                /// Node parent.  if 0xFFFFFFFF, it's the top node.  Maybe...
                /// </summary>
                public UInt32 ParentNodeID { get; internal set; }
                public UInt32 __NumChildren;
                /// <summary>
                /// Material ID for this chunk
                /// </summary>
                public UInt32 MatID { get; internal set; }
                public Boolean IsGroupHead { get; internal set; }
                public Boolean IsGroupMember { get; internal set; }
                /// <summary>
                /// Padding - 2 Bytes
                /// </summary>
                public Byte[] __Reserved1 { get; internal set; }
                private UInt32 __Filler;
                /// <summary>
                /// Transformation Matrix
                /// </summary>
                public Matrix44 Transform { get; internal set; }
                /// <summary>
                /// Position vector of Transform
                /// </summary>
                public Vector3 Pos { get; internal set; }
                /// <summary>
                /// Rotation component of Transform
                /// </summary>
                public Quat Rot { get; internal set; }
                /// <summary>
                /// Scalar component of Transform
                /// </summary>
                public Vector3 Scale { get; internal set; }
                /// <summary>
                /// Position Controller ID
                /// </summary>
                public UInt32 PosCtrl { get; internal set; }
                /// <summary>
                /// Rotation Controller ID
                /// </summary>
                public UInt32 RotCtrl { get; internal set; }
                /// <summary>
                /// Scalar Controller ID
                /// </summary>
                public UInt32 SclCtrl { get; internal set; }

                // These are children, materials, etc.
                public ChunkMtlName MaterialChunk { get; internal set; }
                public ChunkNode[] NodeChildren { get; internal set; }
                // public String NodeFile { get; internal set; }
                // public String ParentNodeName { get; internal set; }

                #endregion

                #region Calculated Properties

                //private ChunkNode _rootNode;
                //public ChunkNode RootNode
                //{
                //    get
                //    {
                //        if (this._rootNode == null && this._model.RootNodeMap.ContainsKey(this.NodeFile))
                //        {
                //            this._rootNode = this._model.RootNodeMap[this.NodeFile];
                //        }

                //        return this._rootNode;
                //    }
                //}
                /// <summary>
                /// Private Data Store for ParentNode
                /// </summary>
                //private ChunkNode _parentNode = null;
                //public ChunkNode ParentNode
                //{
                //    get
                //    {
                //        // Cache the results of the lazy load
                //        if ((this._parentNode == null) && (this.ID != this.RootNode.ID))
                //        {
                //            Chunk tempChunk = null;

                //            if (this.ParentNodeID == 0xFFFFFFFF)
                //            {
                //                tempChunk = this.RootNode;
                //            }
                //            else
                //            {
                //                tempChunk = this._model.ChunksByName.Values.Where(c => c.Name == this.ParentNodeName).FirstOrDefault() ?? this.RootNode;
                //            }
                            
                //            this._parentNode = tempChunk as ChunkNode;
                //        }

                //        return this._parentNode;
                //    }
                //    set
                //    {
                //        this._parentNode = value;
                //        if (this._parentNode != null)
                //        {
                //            this.ParentNodeName = this._parentNode.Name;
                //            this.ParentNodeID = this._parentNode.ID;
                //        }
                //    }
                //}

                private ChunkNode _parentNode;
                public ChunkNode ParentNode
                {
                    get
                    {
                        if (this.ParentNodeID == 0xFFFFFFFF)
                            return null;

                        if (this._parentNode == null)
                        {
                            if (this._model.ChunkMap.ContainsKey(this.ParentNodeID))
                                this._parentNode = this._model.ChunkMap[this.ParentNodeID] as ChunkNode;
                            else
                                this._parentNode = this._model.RootNode;
                        }

                        return this._parentNode;
                    }
                    set { this._parentNode = value; }
                }

                public Vector3 TransformSoFar
                {
                    get
                    {
                        if (this.ParentNode != null)
                        {
                            return this.ParentNode.TransformSoFar.Add(this.Transform.GetTranslation());
                        }
                        else
                        {
                            // TODO: What should this be?
                            return this._model.RootNode.Transform.GetTranslation();
                            // return this.Transform.GetTranslation();
                        }
                    }
                }
                public Matrix33 RotSoFar
                {
                    get
                    {
                        if (this.ParentNode != null)
                        {
                            return this.Transform.To3x3().Mult(this.ParentNode.RotSoFar);
                        }
                        else
                        {
                            return this._model.RootNode.Transform.To3x3();
                            // TODO: What should this be?
                            // return this.Transform.To3x3();
                        }
                    }
                }

                #endregion

                #region Constructor/s

                public ChunkNode(CryEngine.Model model) : base(model) { }

                #endregion

                #region Methods

                /// <summary>
                /// Gets the transform of the vertex.  This will be both the rotation and translation of this vertex, plus all the parents.
                /// 
                /// The transform matrix is a 4x4 matrix.  Vector3 is a 3x1.  We need to convert vector3 to vector4, multiply the matrix, then convert back to vector3.
                /// </summary>
                /// <param name="transform"></param>
                /// <returns></returns>
                public Vector3 GetTransform(Vector3 transform)
                {
                    Vector3 vec3 = transform;

                    // if (this.id != 0xFFFFFFFF)
                    // {

                    // Apply the local transforms (rotation and translation) to the vector
                    // Do rotations.  Rotations must come first, then translate.
                    vec3 = this.RotSoFar.Mult3x1(vec3);
                    // Do translations.  I think this is right.  Objects in right place, not rotated right.
                    vec3 = vec3.Add(this.TransformSoFar);

                    //}

                    return vec3;
                }

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpNodeChunk = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpNodeChunk);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();
                        this.ID = b.ReadUInt32();
                    }
                    // Read the Name string
                    Char[] tmpName = new Char[64];
                    tmpName = b.ReadChars(64);
                    Int32 stringLength = 0;
                    for (Int32 i = 0; i < tmpName.Length; i++)
                    {
                        if (tmpName[i] == 0)
                        {
                            stringLength = i;
                            break;
                        }
                    }
                    this.Name = new string(tmpName, 0, stringLength);
                    this.Object = b.ReadUInt32(); // Object reference ID
                    this.ParentNodeID = b.ReadUInt32();
                    //Console.WriteLine("Node chunk:  {0}. ", Name);
                    if (this.ParentNodeID == 0xFFFFFFFF)
                    {
                        Console.WriteLine("Found Node with Parent == 0xFFFFFFFF.  Name:  {0}", Name);
                    }

                    this.__NumChildren = b.ReadUInt32();
                    this.MatID = b.ReadUInt32();  // Material ID?
                    b.BaseStream.Seek(4, SeekOrigin.Current); // this.__Filler = b.ReadUInt32();  // Actually a couple of booleans and a padding

                    // Read the 4x4 transform matrix.  Should do a couple of for loops, but data structures...
                    this.Transform = new Matrix44
                    {
                        m11 = b.ReadSingle(),
                        m12 = b.ReadSingle(),
                        m13 = b.ReadSingle(),
                        m14 = b.ReadSingle(),
                        m21 = b.ReadSingle(),
                        m22 = b.ReadSingle(),
                        m23 = b.ReadSingle(),
                        m24 = b.ReadSingle(),
                        m31 = b.ReadSingle(),
                        m32 = b.ReadSingle(),
                        m33 = b.ReadSingle(),
                        m34 = b.ReadSingle(),
                        m41 = b.ReadSingle(),
                        m42 = b.ReadSingle(),
                        m43 = b.ReadSingle(),
                        m44 = b.ReadSingle(),
                    };

                    // Read the position Pos Vector3
                    this.Pos = new Vector3
                    {
                        x = b.ReadSingle() / 100,
                        y = b.ReadSingle() / 100,
                        z = b.ReadSingle() / 100,
                    };

                    // Read the rotation Rot Quad
                    this.Rot = new Quat
                    {
                        w = b.ReadSingle(),
                        x = b.ReadSingle(),
                        y = b.ReadSingle(),
                        z = b.ReadSingle(),
                    };

                    // Read the Scale Vector 3
                    this.Scale = new Vector3
                    {
                        x = b.ReadSingle(),
                        y = b.ReadSingle(),
                        z = b.ReadSingle(),
                    };

                    // read the controller pos/rot/scale
                    this.PosCtrl = b.ReadUInt32();
                    this.RotCtrl = b.ReadUInt32();
                    this.SclCtrl = b.ReadUInt32();

                    // Good enough for now.
                }
                public override void WriteChunk()
                {
                    Console.WriteLine("*** START Node Chunk ***");
                    Console.WriteLine("    ChunkType:           {0}", ChunkType);
                    Console.WriteLine("    Node ID:             {0:X}", ID);
                    Console.WriteLine("    Node Name:           {0}", Name);
                    Console.WriteLine("    Object ID:           {0:X}", Object);
                    Console.WriteLine("    Parent ID:           {0:X}", ParentNodeID);
                    Console.WriteLine("    Number of Children:  {0}", __NumChildren);
                    Console.WriteLine("    Material ID:         {0:X}", MatID); // 0x1 is mtllib w children, 0x10 is mtl no children, 0x18 is child
                    Console.WriteLine("    Position:            {0:F7}   {1:F7}   {2:F7}", Pos.x, Pos.y, Pos.z);
                    Console.WriteLine("    Scale:               {0:F7}   {1:F7}   {2:F7}", Scale.x, Scale.y, Scale.z);
                    Console.WriteLine("    Transformation:      {0:F7}  {1:F7}  {2:F7}  {3:F7}", Transform.m11, Transform.m12, Transform.m13, Transform.m14);
                    Console.WriteLine("                         {0:F7}  {1:F7}  {2:F7}  {3:F7}", Transform.m21, Transform.m22, Transform.m23, Transform.m24);
                    Console.WriteLine("                         {0:F7}  {1:F7}  {2:F7}  {3:F7}", Transform.m31, Transform.m32, Transform.m33, Transform.m34);
                    Console.WriteLine("                         {0:F7}  {1:F7}  {2:F7}  {3:F7}", Transform.m41 / 100, Transform.m42 / 100, Transform.m43 / 100, Transform.m44);
                    Console.WriteLine("    Transform_sum:       {0:F7}  {1:F7}  {2:F7}", TransformSoFar.x, TransformSoFar.y, TransformSoFar.z);
                    Console.WriteLine("    Rotation_sum:");
                    RotSoFar.WriteMatrix33();
                    Console.WriteLine("*** END Node Chunk ***");
                }
                
                #endregion
            }

            public class ChunkController : Chunk    // cccc000d:  Controller chunk
            {
                public CtrlType ControllerType;
                public UInt32 NumKeys;
                public UInt32 ControllerFlags;        // technically a bitstruct to identify a cycle or a loop.
                public UInt32 ControllerID;           // Unique id based on CRC32 of bone name.  Ver 827 only?
                public Key[] Keys;                  // array length NumKeys.  Ver 827?

                #region Constructor/s

                public ChunkController(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpChkType = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChkType);
                        this.Version = b.ReadUInt32();  //0x00000918 is Far Cry, Crysis, MWO, Aion
                        this.Offset = b.ReadUInt32();
                        this.ID = b.ReadUInt32();
                    }
                    //Console.WriteLine("ID is:  {0}", id);
                    this.ControllerType = (CtrlType)Enum.ToObject(typeof(CtrlType), b.ReadUInt32());
                    this.NumKeys = b.ReadUInt32();
                    this.ControllerFlags = b.ReadUInt32();
                    this.ControllerID = b.ReadUInt32();
                    this.Keys = new Key[NumKeys];

                    for (Int32 i = 0; i < this.NumKeys; i++)
                    {
                        // Will implement fully later.  Not sure I understand the structure, or if it's necessary.
                        this.Keys[i].Time = b.ReadInt32();
                        // Console.WriteLine("Time {0}", Keys[i].Time);
                        this.Keys[i].AbsPos.x = b.ReadSingle();
                        this.Keys[i].AbsPos.y = b.ReadSingle();
                        this.Keys[i].AbsPos.z = b.ReadSingle();
                        // Console.WriteLine("Abs Pos: {0:F7}  {1:F7}  {2:F7}", Keys[i].AbsPos.x, Keys[i].AbsPos.y, Keys[i].AbsPos.z);
                        this.Keys[i].RelPos.x = b.ReadSingle();
                        this.Keys[i].RelPos.y = b.ReadSingle();
                        this.Keys[i].RelPos.z = b.ReadSingle();
                        // Console.WriteLine("Rel Pos: {0:F7}  {1:F7}  {2:F7}", Keys[i].RelPos.x, Keys[i].RelPos.y, Keys[i].RelPos.z);
                    }
                }

                public override void WriteChunk()
                {
                    Console.WriteLine("*** Controller Chunk ***");
                    Console.WriteLine("Version:                 {0:X}", Version);
                    Console.WriteLine("ID:                      {0:X}", ID);
                    Console.WriteLine("Number of Keys:          {0}", NumKeys);
                    Console.WriteLine("Controller Type:         {0}", ControllerType);
                    Console.WriteLine("Conttroller Flags:       {0}", ControllerFlags);
                    Console.WriteLine("Controller ID:           {0}", ControllerID);
                    for (Int32 i = 0; i < NumKeys; i++)
                    {
                        Console.WriteLine("        Key {0}:       Time: {1}", i, Keys[i].Time);
                        Console.WriteLine("        AbsPos {0}:    {1:F7}, {2:F7}, {3:F7}", i, Keys[i].AbsPos.x, Keys[i].AbsPos.y, Keys[i].AbsPos.z);
                        Console.WriteLine("        RelPos {0}:    {1:F7}, {2:F7}, {3:F7}", i, Keys[i].RelPos.x, Keys[i].RelPos.y, Keys[i].RelPos.z);
                    }
                }
            }

            public class ChunkExportFlags : Chunk  // cccc0015:  Export Flags
            {
                public UInt32 ChunkOffset;  // for some reason the offset of Export Flag chunk is stored here.
                public UInt32 Flags;    // ExportFlags type technically, but it's just 1 value
                public UInt32[] RCVersion;  // 4 uints
                public Char[] RCVersionString;  // Technically String16

                #region Constructor/s

                public ChunkExportFlags(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    UInt32 tmpExportFlag = b.ReadUInt32();
                    this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpExportFlag);
                    this.Version = b.ReadUInt32();
                    this.ChunkOffset = b.ReadUInt32();
                    this.ID = b.ReadUInt32();
                    
                    b.BaseStream.Seek(4, SeekOrigin.Current);
                    
                    this.RCVersion = new uint[4];
                    Int32 count = 0;
                    for (count = 0; count < 4; count++)
                    {
                        this.RCVersion[count] = b.ReadUInt32();
                    }
                    this.RCVersionString = new Char[16];
                    this.RCVersionString = b.ReadChars(16);

                    b.BaseStream.Seek(128, SeekOrigin.Current);
                }
                public override void WriteChunk()
                {
                    String tmpVersionString = new string(RCVersionString);
                    Console.WriteLine("*** START EXPORT FLAGS ***");
                    Console.WriteLine("    Export Chunk ID: {0:X}", ID);
                    Console.WriteLine("    ChunkType: {0}", ChunkType);
                    Console.WriteLine("    Version: {0}", Version);
                    Console.WriteLine("    Flags: {0}", Flags);
                    Console.Write("    RC Version: ");
                    for (Int32 i = 0; i < 4; i++)
                    {
                        Console.Write(RCVersion[i]);
                    }
                    Console.WriteLine();
                    Console.WriteLine("    RCVersion String: {0}", tmpVersionString);
                    Console.WriteLine("*** END EXPORT FLAGS ***");
                }
            }

            public class ChunkSourceInfo : Chunk  // cccc0013:  Source Info chunk.  Pretty useless overall
            {
                public String SourceFile;
                public String Date;
                public String Author;

                #region Constructor/s

                public ChunkSourceInfo(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    this.ChunkType = ChunkTypeEnum.SourceInfo; // this chunk doesn't actually have the chunktype header.
                    // you'd think ReadString() would read from the current offset to the next null byte, but IT DOESN'T.
                    Int32 count = 0;                      // read original file
                    while (b.ReadChar() != 0)
                    {
                        count++;
                    } // count now has the null position relative to the seek position
                    b.BaseStream.Seek(hdr.Offset, 0);
                    Char[] tmpSource = new Char[count];
                    tmpSource = b.ReadChars(count + 1);
                    this.SourceFile = new String(tmpSource);

                    count = 0;                          // Read date
                    while (b.ReadChar() != 0)
                    {
                        count++;
                    } // count now has the null position relative to the seek position
                    b.BaseStream.Seek(b.BaseStream.Position - count - 1, 0);
                    Char[] tmpDate = new Char[count];
                    tmpDate = b.ReadChars(count + 1);  //strip off last 2 Characters, because it contains a return
                    this.Date = new string(tmpDate);

                    count = 0;                           // Read Author
                    while (b.ReadChar() != 0)
                    {
                        count++;
                    } // count now has the null position relative to the seek position
                    b.BaseStream.Seek(b.BaseStream.Position - count - 1, 0);
                    Char[] tmpAuthor = new Char[count];
                    tmpAuthor = b.ReadChars(count + 1);
                    this.Author = new string(tmpAuthor);
                }

                public override void WriteChunk()
                {
                    Console.WriteLine("*** SOURCE INFO CHUNK ***");
                    Console.WriteLine("    ID: {0:X}", ID);
                    Console.WriteLine("    Sourcefile: {0}.  Length {1}", SourceFile, SourceFile.Length);
                    Console.WriteLine("    Date:       {0}.  Length {1}", Date, Date.Length);
                    Console.WriteLine("    Author:     {0}.  Length {1}", Author, Author.Length);
                    Console.WriteLine("*** END SOURCE INFO CHUNK ***");
                }
            }

            public class ChunkMtlName : Chunk  // cccc0014:  provides material name as used in the .mtl file
            {
                // need to find the material ID used by the mesh subsets
                public UInt32 Flags1;  // pointer to the start of this chunk?
                public UInt32 MatType; // for type 800, 0x1 is material library, 0x12 is child, 0x10 is solo material
                //public UInt32 NumChildren802; // for type 802, NumChildren
                public String Name; // technically a String128 class
                public MtlNamePhysicsType PhysicsType; // enum of a 4 byte UInt32  For 802 it's an array, 800 a single element.
                public MtlNamePhysicsType[] PhysicsTypeArray; // enum of a 4 byte UInt32  For 802 it's an array, 800 a single element.
                public UInt32 NumChildren; // number of materials in this name. Max is 66
                // need to implement an array of references here?  Name of Children
                public UInt32[] Children;
                public UInt32[] Padding;  // array length of 32
                public UInt32 AdvancedData;  // probably not used
                public Single Opacity; // probably not used
                public Int32[] Reserved;  // array length of 32

                #region Constructor/s

                public ChunkMtlName(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpChunkMtlName = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChunkMtlName);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();  // offset to this chunk
                        this.ID = b.ReadUInt32();  // ref/chunk number
                    }

                    Char[] tmpName = new Char[128];
                    Int32 stringLength = 0;
                    
                    switch (this.Version)
                    {
                    // at this point we need to differentiate between Version 800 and 802, since the format differs.
                        case 0x800:
                        case 0x744: // guessing on the 744. Aion.
                            this.MatType = b.ReadUInt32();  // if 0x1, then material lib.  If 0x12, mat name.  This is actually a bitstruct.
                            b.BaseStream.Seek(4, SeekOrigin.Current);
                            // read the material Name, which is a 128 byte Char array.  really want it as a string...
                            // long tmpPointer = b.BaseStream.Position;
                            tmpName = b.ReadChars(128);
                            for (Int32 i = 0; i < tmpName.Length; i++)
                            {
                                if (tmpName[i] == 0)
                                {
                                    stringLength = i;
                                    break;
                                }
                            }
                            this.Name = new string(tmpName, 0, stringLength);
                            this.PhysicsType = (MtlNamePhysicsType)Enum.ToObject(typeof(MtlNamePhysicsType), b.ReadUInt32());
                            this.NumChildren = b.ReadUInt32();
                            // Now we need to read the Children references.  2 parts; the number of children, and then 66 - numchildren padding
                            this.Children = new uint[NumChildren];
                            for (Int32 i = 0; i < this.NumChildren; i++)
                            {
                                this.Children[i] = b.ReadUInt32();
                            }
                            // Now dump the rest of the padding
                            this.Padding = new uint[66 - this.NumChildren];
                            for (Int32 i = 0; i < 66 - this.NumChildren; i++)
                            {
                                this.Padding[i] = b.ReadUInt32();
                            }
                        break;
                        case 0x802:
                            // Don't need fillers for this type, but there are no children.
                            Console.WriteLine("version 0x802 material file found....");
                            tmpName = b.ReadChars(128);
                            for (Int32 i = 0; i < tmpName.Length; i++)
                            {
                                if (tmpName[i] == 0)
                                {
                                    stringLength = i;
                                    break;
                                }
                            }
                            this.Name = new string(tmpName, 0, stringLength);
                            this.NumChildren = b.ReadUInt32();  // number of materials
                            this.PhysicsTypeArray = new MtlNamePhysicsType[NumChildren];
                            for (Int32 i = 0; i < NumChildren; i++)
                            {
                                this.PhysicsTypeArray[i] = (MtlNamePhysicsType)Enum.ToObject(typeof(MtlNamePhysicsType), b.ReadUInt32());
                            }
                            break;
                    }

                    // chunkMtlName = this;
                }
                public override void WriteChunk()
                {
                    Console.WriteLine("*** START MATERIAL NAMES ***");
                    Console.WriteLine("    ChunkType:           {0}", ChunkType);
                    Console.WriteLine("    Material Name:       {0}", Name);
                    Console.WriteLine("    Material ID:         {0:X}", ID);
                    Console.WriteLine("    Version:             {0:X}", Version);
                    Console.WriteLine("    Number of Children:  {0}", NumChildren);
                    Console.WriteLine("    Material Type:       {0:X}", MatType); // 0x1 is mtllib w children, 0x10 is mtl no children, 0x18 is child
                    Console.WriteLine("    Physics Type:        {0}", PhysicsType);
                    Console.WriteLine("*** END MATERIAL NAMES ***");
                }
            }

            public class ChunkDataStream : Chunk // cccc0016:  Contains data such as vertices, normals, etc.
            {
                public UInt32 Flags; // not used, but looks like the start of the Data Stream chunk
                public UInt32 Flags1; // not used.  UInt32 after Flags that looks like offsets
                public UInt32 Flags2; // not used, looks almost like a filler.
                public DataStreamTypeEnum DataStreamType; // type of data (vertices, normals, uv, etc)
                public UInt32 NumElements; // Number of data entries
                public UInt32 BytesPerElement; // Bytes per data entry
                public UInt32 Reserved1;
                public UInt32 Reserved2;
                // Need to be careful with using float for Vertices and normals.  technically it's a floating point of length BytesPerElement.  May need to fix this.
                public Vector3[] Vertices;  // For dataStreamType of 0, length is NumElements. 
                public Vector3[] Normals;   // For dataStreamType of 1, length is NumElements.

                public UV[] UVs;            // for datastreamType of 2, length is NumElements.
                public IRGB[] RGBColors;    // for dataStreamType of 3, length is NumElements.  Bytes per element of 3
                public IRGBA[] RGBAColors;  // for dataStreamType of 4, length is NumElements.  Bytes per element of 4
                public UInt32[] Indices;    // for dataStreamType of 5, length is NumElements.
                // For Tangents on down, this may be a 2 element array.  See line 846+ in cgf.xml
                public Tangent[,] Tangents;  // for dataStreamType of 6, length is NumElements,2.  
                public Byte[,] ShCoeffs;     // for dataStreamType of 7, length is NumElement,BytesPerElements.
                public Byte[,] ShapeDeformation; // for dataStreamType of 8, length is NumElements,BytesPerElement.
                public Byte[,] BoneMap;      // for dataStreamType of 9, length is NumElements,BytesPerElement.
                public Byte[,] FaceMap;      // for dataStreamType of 10, length is NumElements,BytesPerElement.
                public Byte[,] VertMats;     // for dataStreamType of 11, length is NumElements,BytesPerElement.

                #region Constructor/s

                public ChunkDataStream() : base(null) { }
                public ChunkDataStream(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpChunkDataStream = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChunkDataStream);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();  // Offset to this chunk
                        this.ID = b.ReadUInt32();  // Reference to the data stream type.
                    }
                    this.Flags2 = b.ReadUInt32(); // another filler
                    UInt32 tmpdataStreamType = b.ReadUInt32();
                    this.DataStreamType = (DataStreamTypeEnum)Enum.ToObject(typeof(DataStreamTypeEnum), tmpdataStreamType);
                    this.NumElements = b.ReadUInt32(); // number of elements in this chunk

                    if (Model.FILE_VERSION == 0)
                    {
                        this.BytesPerElement = b.ReadUInt32(); // bytes per element
                    }
                    if (Model.FILE_VERSION == 1)
                    {
                        this.BytesPerElement = (UInt32)b.ReadInt16();        // Star Citizen 2.0 is using an int16 here now.
                        b.ReadInt16();                                  // unknown value.   Doesn't look like padding though.
                    }

                    b.BaseStream.Seek(8, SeekOrigin.Current);

                    // Now do loops to read for each of the different Data Stream Types.  If vertices, need to populate Vector3s for example.
                    switch (this.DataStreamType)
                    {
                        #region case DataStreamTypeEnum.VERTICES:

                        case DataStreamTypeEnum.VERTICES:  // Ref is 0x00000000
                            this.Vertices = new Vector3[this.NumElements];

                            switch (this.BytesPerElement)
                            {
                                case 12:
                                    for (Int32 i = 0; i < this.NumElements; i++)
                                    {
                                        this.Vertices[i].x = b.ReadSingle();
                                        this.Vertices[i].y = b.ReadSingle();
                                        this.Vertices[i].z = b.ReadSingle();
                                    }
                                    break;
                                case 8:  // Old Star Citizen files
                                    for (Int32 i = 0; i < NumElements; i++)
                                    {
                                        // 2 byte floats.  Use the Half structure from TK.Math

                                        Half xshort = new Half();
                                        xshort.bits = b.ReadUInt16();
                                        this.Vertices[i].x = xshort.ToSingle();

                                        Half yshort = new Half();
                                        yshort.bits = b.ReadUInt16();
                                        this.Vertices[i].y = yshort.ToSingle();

                                        Half zshort = new Half();
                                        zshort.bits = b.ReadUInt16();
                                        this.Vertices[i].z = zshort.ToSingle();

                                        Half wshort = new Half();
                                        wshort.bits = b.ReadUInt16();
                                        this.Vertices[i].w = wshort.ToSingle();
                                    }
                                    break;
                                case 16:  // new Star Citizen files
                                    for (Int32 i = 0; i < this.NumElements; i++)
                                    {
                                        this.Vertices[i].x = b.ReadSingle();
                                        this.Vertices[i].y = b.ReadSingle();
                                        this.Vertices[i].z = b.ReadSingle();
                                        this.Vertices[i].w = b.ReadSingle(); // Sometimes there's a W to these structures.  Will investigate.
                                    }
                                    break;
                            }
                            break;

                        #endregion
                        #region case DataStreamTypeEnum.INDICES:

                        case DataStreamTypeEnum.INDICES:  // Ref is 
                            this.Indices = new UInt32[NumElements];

                            if (this.BytesPerElement == 2)
                            {
                                for (Int32 i = 0; i < this.NumElements; i++)
                                {
                                    this.Indices[i] = (UInt32)b.ReadUInt16();
                                }
                            }
                            if (this.BytesPerElement == 4)
                            {
                                for (Int32 i = 0; i < this.NumElements; i++)
                                {
                                    this.Indices[i] = b.ReadUInt32();
                                }
                            }
                            //Console.WriteLine("Offset is {0:X}", b.BaseStream.Position);
                            break;

                        #endregion
                        #region case DataStreamTypeEnum.NORMALS:

                        case DataStreamTypeEnum.NORMALS:
                            this.Normals = new Vector3[this.NumElements];
                            for (Int32 i = 0; i < NumElements; i++)
                            {
                                this.Normals[i].x = b.ReadSingle();
                                this.Normals[i].y = b.ReadSingle();
                                this.Normals[i].z = b.ReadSingle();
                            }
                            //Console.WriteLine("Offset is {0:X}", b.BaseStream.Position);
                            break;

                        #endregion
                        #region case DataStreamTypeEnum.UVS:

                        case DataStreamTypeEnum.UVS:
                            this.UVs = new UV[this.NumElements];
                            for (Int32 i = 0; i < this.NumElements; i++)
                            {
                                this.UVs[i].U = b.ReadSingle();
                                this.UVs[i].V = b.ReadSingle();
                            }
                            //Console.WriteLine("Offset is {0:X}", b.BaseStream.Position);
                            break;

                        #endregion
                        #region case DataStreamTypeEnum.TANGENTS:

                        case DataStreamTypeEnum.TANGENTS:
                            this.Tangents = new Tangent[this.NumElements, 2];
                            for (Int32 i = 0; i < this.NumElements; i++)
                            {
                                // These have to be divided by 32767 to be used properly (value between 0 and 1)
                                this.Tangents[i, 0].x = b.ReadInt16();
                                this.Tangents[i, 0].y = b.ReadInt16();
                                this.Tangents[i, 0].z = b.ReadInt16();
                                this.Tangents[i, 0].w = b.ReadInt16();

                                this.Tangents[i, 1].x = b.ReadInt16();
                                this.Tangents[i, 1].y = b.ReadInt16();
                                this.Tangents[i, 1].z = b.ReadInt16();
                                this.Tangents[i, 1].w = b.ReadInt16();
                            }
                            // Console.WriteLine("Offset is {0:X}", b.BaseStream.Position);
                            break;

                        #endregion
                        #region case DataStreamTypeEnum.COLORS:

                        case DataStreamTypeEnum.COLORS:
                            if (this.BytesPerElement == 3)
                            {
                                this.RGBColors = new IRGB[this.NumElements];
                                for (Int32 i = 0; i < NumElements; i++)
                                {
                                    this.RGBColors[i].r = b.ReadByte();
                                    this.RGBColors[i].g = b.ReadByte();
                                    this.RGBColors[i].b = b.ReadByte();
                                }
                            }
                            if (this.BytesPerElement == 4)
                            {
                                this.RGBAColors = new IRGBA[this.NumElements];
                                for (Int32 i = 0; i < this.NumElements; i++)
                                {
                                    this.RGBAColors[i].r = b.ReadByte();
                                    this.RGBAColors[i].g = b.ReadByte();
                                    this.RGBAColors[i].b = b.ReadByte();
                                    this.RGBAColors[i].a = b.ReadByte();
                                }
                            }
                            break;

                        #endregion
                        #region case DataStreamTypeEnum.VERTSUVS:

                        case DataStreamTypeEnum.VERTSUVS:  // 3 half floats for verts, 6 unknown, 2 half floats for UVs
                            // Console.WriteLine("In VertsUVs...");
                            this.Vertices = new Vector3[this.NumElements];
                            this.Normals = new Vector3[this.NumElements];
                            this.UVs = new UV[this.NumElements];
                            if (this.BytesPerElement == 16)  // new Star Citizen files
                            {
                                for (Int32 i = 0; i < this.NumElements; i++)
                                {
                                    Half xshort = new Half();
                                    xshort.bits = b.ReadUInt16();
                                    this.Vertices[i].x = xshort.ToSingle();

                                    Half yshort = new Half();
                                    yshort.bits = b.ReadUInt16();
                                    this.Vertices[i].y = yshort.ToSingle();

                                    Half zshort = new Half();
                                    zshort.bits = b.ReadUInt16();
                                    this.Vertices[i].z = zshort.ToSingle();

                                    Half xnorm = new Half();
                                    xnorm.bits = b.ReadUInt16();
                                    this.Normals[i].x = xnorm.ToSingle();

                                    Half ynorm = new Half();
                                    ynorm.bits = b.ReadUInt16();
                                    this.Normals[i].y = ynorm.ToSingle();

                                    Half znorm = new Half();
                                    znorm.bits = b.ReadUInt16();
                                    this.Normals[i].z = znorm.ToSingle();

                                    Half uvu = new Half();
                                    uvu.bits = b.ReadUInt16();
                                    this.UVs[i].U = uvu.ToSingle();

                                    Half uvv = new Half();
                                    uvv.bits = b.ReadUInt16();
                                    this.UVs[i].V = uvv.ToSingle();

                                    //short w = b.ReadInt16();  // dump this as not needed.  Last 2 bytes are surplus...sort of.
                                    //if (i < 20)
                                    //{
                                    //    Console.WriteLine("{0:F7} {1:F7} {2:F7} {3:F7} {4:F7}",
                                    //        Vertices[i].x, Vertices[i].y, Vertices[i].z,
                                    //        UVs[i].U, UVs[i].V);
                                    //}
                                }
                            }
                            break;

                        #endregion
                        #region default:

                        default:
                            Console.WriteLine("***** Unknown DataStream Type *****");
                            break;

                        #endregion
                    }
                }

                public override void WriteChunk()
                {
                    //string tmpDataStream = new string(Name);
                    Console.WriteLine("*** START DATASTREAM ***");
                    Console.WriteLine("    ChunkType:                       {0}", ChunkType);
                    Console.WriteLine("    Version:                         {0:X}", Version);
                    Console.WriteLine("    DataStream chunk starting point: {0:X}", Flags);
                    Console.WriteLine("    Chunk ID:                        {0:X}", ID);
                    Console.WriteLine("    DataStreamType:                  {0}", DataStreamType);
                    Console.WriteLine("    Number of Elements:              {0}", NumElements);
                    Console.WriteLine("    Bytes per Element:               {0}", BytesPerElement);
                    Console.WriteLine("*** END DATASTREAM ***");

                }
            }

            public class ChunkMeshSubsets : Chunk // cccc0017:  The different parts of a mesh.  Needed for obj exporting
            {
                public UInt32 Flags; // probably the offset
                public UInt32 NumMeshSubset; // number of mesh subsets
                public MeshSubset[] MeshSubsets;

                #region Constructor/s

                public ChunkMeshSubsets(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpChunkType = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChunkType);
                        this.Version = b.ReadUInt32(); // probably 800
                        this.Offset = b.ReadUInt32();  // offset to this chunk
                        this.ID = b.ReadUInt32(); // ID of this chunk.  Used to reference the mesh chunk
                        this.Flags = b.ReadUInt32();   // Might be a ref to this chunk
                        this.NumMeshSubset = b.ReadUInt32();  // number of mesh subsets
                        b.BaseStream.Seek(8, SeekOrigin.Current);
                        this.MeshSubsets = new MeshSubset[NumMeshSubset];
                        for (Int32 i = 0; i < NumMeshSubset; i++)
                        {
                            this.MeshSubsets[i].FirstIndex = b.ReadUInt32();
                            this.MeshSubsets[i].NumIndices = b.ReadUInt32();
                            this.MeshSubsets[i].FirstVertex = b.ReadUInt32();
                            this.MeshSubsets[i].NumVertices = b.ReadUInt32();
                            this.MeshSubsets[i].MatID = b.ReadUInt32();
                            this.MeshSubsets[i].Radius = b.ReadSingle();
                            this.MeshSubsets[i].Center.x = b.ReadSingle();
                            this.MeshSubsets[i].Center.y = b.ReadSingle();
                            this.MeshSubsets[i].Center.z = b.ReadSingle();
                        }
                    }
                    if (Model.FILE_VERSION == 1)  // 3.6 and newer files
                    {
                        this.Flags = b.ReadUInt32();   // Might be a ref to this chunk
                        this.NumMeshSubset = b.ReadUInt32();  // number of mesh subsets
                        b.BaseStream.Seek(8, SeekOrigin.Current);
                        this.MeshSubsets = new MeshSubset[NumMeshSubset];
                        for (Int32 i = 0; i < NumMeshSubset; i++)
                        {
                            this.MeshSubsets[i].FirstIndex = b.ReadUInt32();
                            this.MeshSubsets[i].NumIndices = b.ReadUInt32();
                            this.MeshSubsets[i].FirstVertex = b.ReadUInt32();
                            this.MeshSubsets[i].NumVertices = b.ReadUInt32();
                            this.MeshSubsets[i].MatID = b.ReadUInt32();
                            this.MeshSubsets[i].Radius = b.ReadSingle();
                            this.MeshSubsets[i].Center.x = b.ReadSingle();
                            this.MeshSubsets[i].Center.y = b.ReadSingle();
                            this.MeshSubsets[i].Center.z = b.ReadSingle();
                        }
                    }
                }
                public override void WriteChunk()
                {
                    Console.WriteLine("*** START MESH SUBSET CHUNK ***");
                    Console.WriteLine("    ChunkType:       {0}", ChunkType);
                    Console.WriteLine("    Mesh SubSet ID:  {0:X}", ID);
                    Console.WriteLine("    Number of Mesh Subsets: {0}", NumMeshSubset);
                    for (Int32 i = 0; i < NumMeshSubset; i++)
                    {
                        Console.WriteLine("        ** Mesh Subset:          {0}", i);
                        Console.WriteLine("           First Index:          {0}", MeshSubsets[i].FirstIndex);
                        Console.WriteLine("           Number of Indices:    {0}", MeshSubsets[i].NumIndices);
                        Console.WriteLine("           First Vertex:         {0}", MeshSubsets[i].FirstVertex);
                        Console.WriteLine("           Number of Vertices:   {0}  (next will be {1})", MeshSubsets[i].NumVertices, MeshSubsets[i].NumVertices + MeshSubsets[i].FirstVertex);
                        Console.WriteLine("           Material ID:          {0}", MeshSubsets[i].MatID);
                        Console.WriteLine("           Radius:               {0}", MeshSubsets[i].Radius);
                        Console.WriteLine("           Center:   {0},{1},{2}", MeshSubsets[i].Center.x, MeshSubsets[i].Center.y, MeshSubsets[i].Center.z);
                        Console.WriteLine("        ** Mesh Subset {0} End", i);
                    }
                    Console.WriteLine("*** END MESH SUBSET CHUNK ***");
                }
            }

            public class ChunkMesh : Chunk      //  cccc0000:  Object that points to the datastream chunk.
            {
                // public UInt32 Version;  // 623 Far Cry, 744 Far Cry, Aion, 800 Crysis
                //public bool HasVertexWeights; // for 744
                //public bool HasVertexColors; // 744
                //public bool InWorldSpace; // 623
                //public byte Reserved1;  // padding byte, 744
                //public byte Reserved2;  // padding byte, 744
                public UInt32 Flags1;  // 800  Offset of this chunk. 
                // public UInt32 ID;  // 800  Chunk ID
                public UInt32 NumVertices; // 
                public UInt32 NumIndices;  // Number of indices (each triangle has 3 indices, so this is the number of triangles times 3).
                //public UInt32 NumUVs; // 744
                //public UInt32 NumFaces; // 744
                // Pointers to various Chunk types
                //public ChunkMtlName Material; // 623, Material Chunk, never encountered?
                public UInt32 NumVertSubsets; // 801, Number of vert subsets
                public UInt32 MeshSubsets; // 800  Reference of the mesh subsets
                // public ChunkVertAnim VertAnims; // 744.  not implemented
                //public Vertex[] Vertices; // 744.  not implemented
                //public Face[,] Faces; // 744.  Not implemented
                //public UV[] UVs; // 744 Not implemented
                //public UVFace[] UVFaces; // 744 not implemented
                // public VertexWeight[] VertexWeights; // 744 not implemented
                //public IRGB[] VertexColors; // 744 not implemented
                public UInt32 VerticesData; // 800, 801.  Need an array because some 801 files have NumVertSubsets
                public UInt32 NumBuffs;
                public UInt32[] Buffer;       // 801.  For some reason there is a weird buffer here.
                public UInt32 NormalsData; // 800
                public UInt32 UVsData; // 800
                public UInt32 ColorsData; // 800
                public UInt32 Colors2Data; // 800 
                public UInt32 IndicesData; // 800
                public UInt32 TangentsData; // 800
                public UInt32 ShCoeffsData; // 800
                public UInt32 ShapeDeformationData; //800
                public UInt32 BoneMapData; //800
                public UInt32 FaceMapData; // 800
                public UInt32 VertMatsData; // 800
                public UInt32 MeshPhysicsData; // 801
                public UInt32 VertsUVsData;    // 801
                public UInt32[] PhysicsData = new uint[4]; // 800
                public Vector3 MinBound; // 800 minimum coordinate values
                public Vector3 MaxBound; // 800 Max coord values

                //public ChunkMeshSubsets chunkMeshSubset; // pointer to the mesh subset that belongs to this mesh

                #region Constructor/s

                public ChunkMesh(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpChunkType = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChunkType);
                        this.Version = b.ReadUInt32();
                        this.Flags1 = b.ReadUInt32();  // offset
                        this.ID = b.ReadUInt32();  // Chunk ID  0x23 for candle
                    }
                    if (Version == 0x800)
                    {
                        this.NumVertSubsets = 1;
                        b.BaseStream.Seek(8, SeekOrigin.Current);
                        this.NumVertices = b.ReadUInt32();
                        this.NumIndices = b.ReadUInt32();   //  Number of indices
                        b.BaseStream.Seek(4, SeekOrigin.Current);
                        this.MeshSubsets = b.ReadUInt32();  // refers to ID in mesh subsets  1d for candle.  Just 1 for 0x800 type
                        b.BaseStream.Seek(4, SeekOrigin.Current);
                        this.VerticesData = b.ReadUInt32();  // ID of the datastream for the vertices for this mesh
                        this.NormalsData = b.ReadUInt32();   // ID of the datastream for the normals for this mesh
                        this.UVsData = b.ReadUInt32();     // refers to the ID in the Normals datastream?
                        this.ColorsData = b.ReadUInt32();
                        this.Colors2Data = b.ReadUInt32();
                        this.IndicesData = b.ReadUInt32();
                        this.TangentsData = b.ReadUInt32();
                        this.ShCoeffsData = b.ReadUInt32();
                        this.ShapeDeformationData = b.ReadUInt32();
                        this.BoneMapData = b.ReadUInt32();
                        this.FaceMapData = b.ReadUInt32();
                        this.VertMatsData = b.ReadUInt32();
                        b.BaseStream.Seek(16, SeekOrigin.Current);
                        for (Int32 i = 0; i < 4; i++)
                        {
                            this.PhysicsData[i] = b.ReadUInt32();
                        }
                        this.MinBound.x = b.ReadSingle();
                        this.MinBound.y = b.ReadSingle();
                        this.MinBound.z = b.ReadSingle();
                        this.MaxBound.x = b.ReadSingle();
                        this.MaxBound.y = b.ReadSingle();
                        this.MaxBound.z = b.ReadSingle();
                    }
                    else if (Version == 0x801)
                    {
                        b.BaseStream.Seek(8, SeekOrigin.Current);
                        this.NumVertices = b.ReadUInt32();
                        this.NumIndices = b.ReadUInt32();
                        b.BaseStream.Seek(4, SeekOrigin.Current);
                        this.MeshSubsets = b.ReadUInt32();  // refers to ID in mesh subsets 
                        b.BaseStream.Seek(4, SeekOrigin.Current);
                        this.VerticesData = b.ReadUInt32();
                        this.NormalsData = b.ReadUInt32();           // ID of the datastream for the normals for this mesh
                        this.UVsData = b.ReadUInt32();               // refers to the ID in the Normals datastream
                        this.ColorsData = b.ReadUInt32();
                        this.Colors2Data = b.ReadUInt32();
                        this.IndicesData = b.ReadUInt32();
                        this.TangentsData = b.ReadUInt32();
                        b.BaseStream.Seek(16, SeekOrigin.Current);
                        for (Int32 i = 0; i < 4; i++)
                        {
                            this.PhysicsData[i] = b.ReadUInt32();
                        }
                        this.VertsUVsData = b.ReadUInt32();  // This should be a vertsUV index number, not vertices.  Vertices are above.
                        this.ShCoeffsData = b.ReadUInt32();
                        this.ShapeDeformationData = b.ReadUInt32();
                        this.BoneMapData = b.ReadUInt32();
                        this.FaceMapData = b.ReadUInt32();
                        this.MinBound.x = b.ReadSingle();
                        this.MinBound.y = b.ReadSingle();
                        this.MinBound.z = b.ReadSingle();
                        this.MaxBound.x = b.ReadSingle();
                        this.MaxBound.y = b.ReadSingle();
                        this.MaxBound.z = b.ReadSingle();
                    }
                }
                public override void WriteChunk()
                {
                    Console.WriteLine("*** START MESH CHUNK ***");
                    Console.WriteLine("    ChunkType:           {0}", ChunkType);
                    Console.WriteLine("    Chunk ID:            {0:X}", ID);
                    Console.WriteLine("    MeshSubSetID:        {0:X}", MeshSubsets);
                    Console.WriteLine("    Vertex Datastream:   {0:X}", VerticesData);
                    Console.WriteLine("    Normals Datastream:  {0:X}", NormalsData);
                    Console.WriteLine("    UVs Datastream:      {0:X}", UVsData);
                    Console.WriteLine("    Indices Datastream:  {0:X}", IndicesData);
                    Console.WriteLine("    Tangents Datastream: {0:X}", TangentsData);
                    Console.WriteLine("    Mesh Physics Data:   {0:X}", MeshPhysicsData);
                    Console.WriteLine("    VertUVs:             {0:X}", VertsUVsData);
                    Console.WriteLine("    MinBound:            {0:F7}, {1:F7}, {2:F7}", MinBound.x, MinBound.y, MinBound.z);
                    Console.WriteLine("    MaxBound:            {0:F7}, {1:F7}, {2:F7}", MaxBound.x, MaxBound.y, MaxBound.z);
                    Console.WriteLine("*** END MESH CHUNK ***");
                }
            }

            public class ChunkSceneProp : Chunk     // cccc0008 
            {
                // This chunk isn't really used, but contains some data probably necessary for the game.
                // Size for 0x744 type is always 0xBB4 (test this)
                public UInt32 NumProps;             // number of elements in the props array  (31 for type 0x744)
                public String[] PropKey;
                public String[] PropValue;

                #region Constructor/s

                public ChunkSceneProp(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    if (Model.FILE_VERSION == 0)
                    {
                        UInt32 tmpChunkType = b.ReadUInt32();
                        this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChunkType);
                        this.Version = b.ReadUInt32();
                        this.Offset = b.ReadUInt32();  // offset
                        this.ID = b.ReadUInt32();
                    }
                    this.NumProps = b.ReadUInt32();          // Should be 31 for 0x744
                    this.PropKey = new String[this.NumProps];
                    this.PropValue = new String[this.NumProps];

                    // Read the array of scene props and their associated values
                    for (Int32 i = 0; i < this.NumProps; i++)
                    {
                        Char[] tmpProp = new Char[32];
                        Char[] tmpPropValue = new Char[64];
                        tmpProp = b.ReadChars(32);
                        Int32 stringLength = 0;
                        for (Int32 j = 0; j < tmpProp.Length; j++)
                        {
                            if (tmpProp[j] == 0)
                            {
                                stringLength = j;
                                break;
                            }
                        }
                        this.PropKey[i] = new String(tmpProp, 0, stringLength);

                        tmpPropValue = b.ReadChars(64);
                        stringLength = 0;
                        for (Int32 j = 0; j < tmpPropValue.Length; j++)
                        {
                            if (tmpPropValue[j] == 0)
                            {
                                stringLength = j;
                                break;
                            }
                        }
                        this.PropValue[i] = new String(tmpPropValue, 0, stringLength);
                    }
                }
                public override void WriteChunk()
                {
                    Console.WriteLine("*** START SceneProp Chunk ***");
                    Console.WriteLine("    ChunkType:   {0}", ChunkType);
                    Console.WriteLine("    Version:     {0:X}", Version);
                    Console.WriteLine("    ID:          {0:X}", ID);
                    for (Int32 i = 0; i < NumProps; i++)
                    {
                        Console.WriteLine("{0,30}{1,20}", PropKey[i], PropValue[i]);
                    }
                    Console.WriteLine("*** END SceneProp Chunk ***");
                }
            }

            public class ChunkTimingFormat : Chunk  // cccc000e:  Timing format chunk
            {
                // This chunk doesn't have an ID, although one may be assigned in the chunk table.
                public Single SecsPerTick;
                public Int32 TicksPerFrame;
                public UInt32 Unknown1; // 4 bytes, not sure what they are
                public UInt32 Unknown2; // 4 bytes, not sure what they are
                public RangeEntity GlobalRange;
                public Int32 NumSubRanges;

                #region Constructor/s

                public ChunkTimingFormat(CryEngine.Model model) : base(model) { }

                #endregion

                public override void ReadChunk(BinaryReader b, ChunkHeader hdr)
                {
                    base.ReadChunk(b, hdr);

                    UInt32 tmpChkType = b.ReadUInt32();
                    this.ChunkType = (ChunkTypeEnum)Enum.ToObject(typeof(ChunkTypeEnum), tmpChkType);
                    this.Version = b.ReadUInt32();  //0x00000918 is Far Cry, Crysis, MWO, Aion, SC
                    this.SecsPerTick = b.ReadSingle();
                    this.TicksPerFrame = b.ReadInt32();
                    this.Unknown1 = b.ReadUInt32();
                    this.Unknown2 = b.ReadUInt32();
                    this.GlobalRange.Name = new Char[32];
                    this.GlobalRange.Name = b.ReadChars(32);  // Name is technically a String32, but F those structs
                    this.GlobalRange.Start = b.ReadInt32();
                    this.GlobalRange.End = b.ReadInt32();
                }
                public override void WriteChunk()
                {
                    String tmpName = new string(GlobalRange.Name);
                    Console.WriteLine("*** TIMING CHUNK ***");
                    Console.WriteLine("    ID: {0:X}", ID);
                    Console.WriteLine("    Version: {0:X}", Version);
                    Console.WriteLine("    Secs Per Tick: {0}", SecsPerTick);
                    Console.WriteLine("    Ticks Per Frame: {0}", TicksPerFrame);
                    Console.WriteLine("    Global Range:  Name: {0}", tmpName);
                    Console.WriteLine("    Global Range:  Start: {0}", GlobalRange.Start);
                    Console.WriteLine("    Global Range:  End:  {0}", GlobalRange.End);
                    Console.WriteLine("*** END TIMING CHUNK ***");
                }
            }

            public class FileSignature          // NYI. The signature that Cryengine files start with.  Crytek or CrChF 
            {
                public String Read(BinaryReader b)  // Checks the signature
                {
                    Char[] signature = new Char[8];  // first 8 bytes are the file signature.
                    signature = b.ReadChars(8);
                    String s = new string(signature);
                    return s;
                }
            }

            #endregion

            #endregion
        }
    }
}