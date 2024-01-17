{
  "Genus": [ "gs_qiandao" ],
  "ShoppingItem": {
    "Period": {
      "Start": "2023-05-09T00:00:00.000Z",
      "End": null,
      "PeriodString": "1d",
      "ValidPeriodString": "1d"
    },
    "GroupNumber": 0,
    "Ins": [
      {
        "Conditional": [
          {
            "ParentTId":null,
            "Genus": [],
            "TId": "占位符tid",
            "MinCount": 0,
            "NumberCondition": {
              "PropertyName": "Count",
              "MinValue": 0,
              "MaxValue": null,
              "Subtrahend": 0,
              "Modulus": 7,
              "MinRemainder": 0,
              "MaxRemainder": 6
            },
            "GeneralConditional": [
            ]
          }
        ],
        "Count": 0,
        "Genus": []
      },
      {
        "Conditional": [
          {
            "ParentTId": "123a5ad1-d4f0-4cd9-9abc-d440419d9e0d",
            "Genus": [],
            "TId": "消耗序列TId",
            "MinCount": 0,
            "NumberCondition": null,
            "GeneralConditional": [
            ]
          }
        ],
        "Count": 0,
        "Genus": []
      }
    ],
    "Outs": [
      {
        "ParentTId": null,
        "TId": "输出序列Tid",
        "Count": 1
      }
    ],
    "MaxCount": 2
  }
}