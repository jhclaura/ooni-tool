using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArtPainting {

	public string name;
	public int width;
	public int height;
	public float scale;

	public ArtPainting(string _name, int _w, int _h){
		name = _name;
		width = _w;
		height = _h;
		scale = (float) height / width;
	}
}
