using UnityEngine;
using System.Collections;

public class PlayerController : PaddleController {

	// Update is called once per frame
	void Update () {
	    if (Input.GetAxis("Vertical") > 0f)
        {
            MoveUp();
        }
        else if (Input.GetAxis("Vertical") < 0f)
        {
            MoveDown();
        }
    }
}
