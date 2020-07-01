﻿/*

	SketchUpForGrasshopper - Trimble(R) SketchUp(R) interface for McNeel's(R) Grasshopper(R) 
	Copyright(C) 2020, Autor: Maximilian Thumfart

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software
    and associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
    subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;
using SketchUpNET;

namespace SketchUpForGrasshopper
{
    /// <summary>
    /// SketchUp Model Component
    /// </summary>
    public class SketchUpModel : GH_Component
    {
        public SketchUpModel() : base("Load SketchUp Model", "Load SketchUp Model", "Loads a SketchUp Model from file", "SketchUpSharp", "Model") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path to Sketchup File (skp)", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Surfaces", "S", "Surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Layers", "L", "Layers", GH_ParamAccess.list);
            pManager.AddGenericParameter("Instances", "I", "Instances", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_String path = new GH_String();
            DA.GetData<GH_String>(0, ref path);

            List<GH_Brep> surfaces = new List<GH_Brep>();
            List<GH_String> layers = new List<GH_String>();
            List<Instance> Instances = new List<Instance>();

            SketchUp skp = new SketchUp();
            if (skp.LoadModel(path.Value))
            { 
                foreach (Surface srf in skp.Surfaces)
                    surfaces.Add(new GH_Brep(srf.ToRhinoGeo()));

                foreach (Layer l in skp.Layers)
                    layers.Add(new GH_String(l.Name));

                foreach (Instance i in skp.Instances)
                    Instances.Add(i);
            }

            DA.SetDataList(0, surfaces);
            DA.SetDataList(1, layers);
            DA.SetDataList(2, Instances);
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("{5ea8ce4d-d268-4d7f-a733-1583beeb4b5d}");
            }
        }
        protected override Bitmap Internal_Icon_24x24
        {
            get
            {
                return Properties.Resources.Skp;
            }
        }
    }

    /// <summary>
    /// Decomposes a SketchUp Model Instance
    /// </summary>
    public class SketchUpInstance : GH_Component
    {
        public SketchUpInstance() : base("Decompose SketchUp Instance", "Decompose SketchUp Instance", "Decomposes a SketchUp Instance", "SketchUpSharp", "Elements") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Instance", "I", "Instance", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Location", "L", "Location", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Name", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale", "S", "Scale", GH_ParamAccess.item);
            pManager.AddBrepParameter("Surfaces", "Sf", "Surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Parent Name", "PN", "Parent Name", GH_ParamAccess.item);
            pManager.AddBrepParameter("Inner", "I", "Inner", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Instance i = null;
            DA.GetData<Instance>(0, ref i);

            GH_Point location = new GH_Point(new Rhino.Geometry.Point3d(i.Transformation.X, i.Transformation.Y, i.Transformation.Z));
            GH_Number scale = new GH_Number(i.Transformation.Scale);
            GH_String name = new GH_String(i.Name);

            List<GH_Brep> surfaces = new List<GH_Brep>();
            List<GH_Brep> inner = new List<GH_Brep>();

            GH_String parentName = new GH_String("");
            SketchUpNET.Component parentComponent = i.Parent as Component;
            if (parentComponent != null)
            {
                parentName = new GH_String(parentComponent.Name);
                foreach (Surface srf in parentComponent.Surfaces)
                    surfaces.Add(new GH_Brep(srf.ToRhinoGeo(i.Transformation)));
            }

            DA.SetData(0, location);
            DA.SetData(1, name);
            DA.SetData(2, scale);
            DA.SetDataList(3, surfaces);
            DA.SetData(4, parentName);
            DA.SetDataList(5, inner);
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("{5ea7ce4d-d266-4d7f-a733-1583beeb4b8d}");
            }
        }
        protected override Bitmap Internal_Icon_24x24
        {
            get
            {
                return Properties.Resources.Skp;
            }
        }
    }

    /// <summary>
    /// Geometry Translations
    /// </summary>
    public static class Geometry
    {
        /// <summary>
        /// Converts a SketchUp Vertex to a Rhino Point
        /// </summary>
        public static Rhino.Geometry.Point3d ToRhinoGeo(this SketchUpNET.Vertex v, Transform t)
        {
            if (t == null)
                return new Rhino.Geometry.Point3d(v.X , v.Y , v.Z );
            else
            {
                Vertex transformed = t.GetTransformed(v);
                return new Rhino.Geometry.Point3d(transformed.X, transformed.Y, transformed.Z);
            }
        }

        /// <summary>
        /// Converts a SketchUp Vector to a Rhino Vector
        /// </summary>
        public static Rhino.Geometry.Vector3d ToRhinoGeo(this SketchUpNET.Vector v)
        {
                return new Rhino.Geometry.Vector3d(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Converts a SketchUp Edge to a Rhino Line
        /// </summary>
        public static Rhino.Geometry.Line ToRhinoGeo(this SketchUpNET.Edge v, Transform t)
        {
            return new Rhino.Geometry.Line(v.Start.ToRhinoGeo(t), v.End.ToRhinoGeo(t));
        }

        /// <summary>
        /// Converts a SketchUp Surface to a Rhino Surface
        /// </summary>
        public static Rhino.Geometry.Brep ToRhinoGeo(this SketchUpNET.Surface v, Transform t = null)
        {
            List<Rhino.Geometry.Curve> curves = new List<Rhino.Geometry.Curve>();
            foreach (SketchUpNET.Edge c in v.OuterEdges.Edges) curves.Add(c.ToRhinoGeo(t).ToNurbsCurve());

            foreach (Loop loop in v.InnerEdges)
                foreach (SketchUpNET.Edge c in loop.Edges) curves.Add(c.ToRhinoGeo(t).ToNurbsCurve());

            Rhino.Geometry.Curve[] crv = Rhino.Geometry.Curve.JoinCurves(curves);

            Rhino.Geometry.Surface b = Rhino.Geometry.Surface.CreateExtrusion(crv[0],v.Normal.ToRhinoGeo());

            List<Rhino.Geometry.Brep> breps = v.InnerLoops(t);

            Rhino.Geometry.Brep result = b.ToBrep();

            if (breps.Count > 0)
            {
                Rhino.Geometry.Brep[] tmp = Rhino.Geometry.Brep.CreateBooleanDifference(new List<Rhino.Geometry.Brep>() { b.ToBrep() }, breps, 0);
                if (tmp.Length > 0) result = tmp[0];
            }
            return result;
        }

        /// <summary>
        /// Converts a SketchUp Surface InnerLoops to a Rhino Geometries
        /// </summary>
        public static List<Rhino.Geometry.Brep> InnerLoops(this SketchUpNET.Surface v, Transform t = null)
        {
            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();

            foreach (Loop loop in v.InnerEdges)
            {
                List<Rhino.Geometry.Curve> curves = new List<Rhino.Geometry.Curve>();
                foreach (SketchUpNET.Edge c in loop.Edges) curves.Add(c.ToRhinoGeo(t).ToNurbsCurve());

                Rhino.Geometry.Curve[] crv = Rhino.Geometry.Curve.JoinCurves(curves);
                Rhino.Geometry.Surface b = Rhino.Geometry.Surface.CreateExtrusion(crv[0], v.Normal.ToRhinoGeo());
                breps.Add(b.ToBrep());
            }
            return breps;
        }
    }
}