using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using Newtonsoft.Json;
using UnityEngine;

public class RobotRangeStorage : Singleton<RobotRangeStorage>
{
    public Dictionary<string, float[]> RobotsRange = new Dictionary<string, float[]>();
    public List<IO.Swagger.Model.ListProjectsResponseData> Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
