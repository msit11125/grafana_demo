- [ES Index規劃](#es-index規劃)
  - [Dynamic Field Mappings](#dynamic-field-mappings)
  - [Index 命名](#index-命名)
  - [資料建立](#資料建立)
  - [新增資料](#新增資料)
  - [資料查詢](#資料查詢)
  - [Index Template](#index-template)
  - [Index alias](#index-alias)
  - [Object Fields VS Nested Field](#object-fields-vs-nested-field)
- [ES優化](#es優化)
- [C# NEST套件](#nest套件)
### ES Index規劃

#### Dynamic Field Mappings
當一個 JSON 文件 indexing 進入 Elasticsearch 時，Dynamic field mapping 會依照 JSON 欄位原本的資料型態，來分別執行判定的規則：

|JSON 的資料型態|判定成為的 Elasticsearch 資料型態|
|----|------|
|null|不會產生這個欄位|
|true or false|boolean|
|浮點數|float|
|整數|long|
|物件|object|
|陣列|依照陣列內的資料型態決定|
|字串|1. 判定這個字串是否為日期格式。2. 判定這個字串是否為 double 或是 long 的格式。3. 若都非以上的格式，會直接指派 text 型態，並搭配 keyword 的 sub-field。|


* keyword存儲數據的時候，不會分詞建立索引。
* text類型在存儲數據的時候會默認進行分詞，並生成索引，並且不允許 sort。

#### Index 命名
aoi-jobresult-history
{模組}-{功能名稱}

- 必要欄位：
"version": 1011000, (uint)
"create_date": "2022-03-21T12:31:04.138Z" (date)

[避免字元](https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-create-index.html)

#### 資料建立

什麽是dynamic? 
[官方解釋](https://www.elastic.co/guide/en/elasticsearch/reference/current/dynamic.html)

簡言之：避免隨意被INSERT不必要的欄位

```bash
# 方法一、 創建Index 然後再設定 Mapping (分開設定)
# dynamic_templates: 統一的欄位的命名規則 
PUT aoi-jobresult-history

PUT aoi-jobresult-history/_mapping
{
   "dynamic_templates": [
    {
      "long_field": {
        "match":   "long_*",
        "mapping": {
          "type": "long"
        }
      }
    },
    {
      "double_field": {
        "match":   "double_*",
        "mapping": {
          "type": "double"
        }
      }
    }
  ],
  "dynamic": false,
  "properties": {
    "group": {
      "type": "text",
       "fields" : {
          "keyword" : {
            "type" : "keyword",
            "ignore_above" : 256
          }
       }
    },
    "createDate": {
      "type":   "date",
      "format": "strict_date_optional_time||epoch_millis"
    },
    "version":{
      "type": "integer"
    },
    "metaData": {
      "dynamic": true, 
      "properties": {}
    }
  }
}

# 方法二、 創建Index + Mapping (一起設定)
PUT aoi-jobresult-history
{
  "mappings": {
    "dynamic_templates": 
    .... 同上
  }
}

```
- dynamic可以設定為：
    - true: 執行 dynamic mapping。
    - false: 不執行 dynamic mapping，並在 indexing 時忽略沒有被宣告的欄位 (但非mapping的資料依然可以被insert)。
    - strict: 不執行 dynamic mapping，並在 indexing 時遇到沒有宣告的欄位會直接拋出 exception。

- 一般時間格式
  ```javascript
  var date = JSON.stringify(new Date())
  console.log(date); // 2022-03-21T12:31:04.138Z
  ```

##### 關閉 日期 或 數值 的自動判斷
```bash
PUT aoi-jobresult-history/_mappings
{
  "date_detection": false,
  "numeric_detection": true
}
```


#### 新增資料

```bash
PUT aoi-jobresult-history/_doc/1   #  Id: 1
{
  "name": "Job 1",
  "group" : "group1 group2",
  "metaData": [{
    "long_a": 100,
    "b": "b",
    "c": [ { "c1": "c1"} ]
  }],
  "version": 1011000,
  "createDate": "2022-03-21T12:31:04.138Z"
}
```

#### 資料查詢
```bash
# Full Text Query
GET /aoi-jobresult-history/_search
{
  "query": {
    "bool": {
       "must": [
        { 
          "match": {
            "name":{
              "query": "Job 1" 
            }
          }
        },
        { 
          "match": {
            "metaData.b":{
              "query": "b" 
            }
          }
        }
      ],
      "filter": {
        "term" : { "group": "group1" }
      }
    }
  }
}

# 準確搜尋: 加上keyoword
{
  ...
        "match": {
            "name.keyword":{
                "query": "Job 1" 
            }
        }
  ...
}
```
- 備註
  - term 是直接把 field 拿去查詢倒排索引中確切的 term
  - match 會先對 field 進行分詞操作，然後再去倒排索引中查詢
  
[其他高階搜尋語法](https://iter01.com/429035.html)

#### Index Template

##### [Index Template](https://www.elastic.co/guide/en/elasticsearch/reference/current/index-templates.html)

預先建立好 Tempalte，而當新的 Index 要被建立時，若符合設定好的 index_patterns，則使用這個 Template裡的設定來建立 Index。

```bash

PUT _index_template/template_aoi
{
  "index_patterns" : ["template-aoi-*"],
  "template": {
    "settings" : {
        "number_of_shards" : 1
    },
    "mappings" : {
      "dynamic": "strict",
      "properties": {
        "name": {
          "type": "text",
           "fields" : {
              "keyword" : {
                "type" : "keyword",
                "ignore_above" : 256
              }
           }
        },
        "group": {
          "type": "text",
           "fields" : {
              "keyword" : {
                "type" : "keyword",
                "ignore_above" : 256
              }
           }
        }
      }
    }
  },
  "priority" : 0,
  "version": 1,
  "_meta": {
    "description": "my custom"
  }
}


```
- index_patterns: 這是指 index 或 data stream 的名字，可以使用萬用字元 * 來定義這個 pattern。

- composed_of: 這是 Elasticsearch 7.8 新增的功能，可以套用事先定義好的 Component Template，這個可以設定多個 Component - Templates，若有重覆的設定值，會以"後面的蓋掉前面的"來進行合併。
- priority: 也是 Elasticsearch 7.8 新增的功能，指定 Index Template 的優先順序，數字愈大愈優先。(若沒有指定，會當成0，也就是最低優先權來處理)
- version: 讓使用者自己編寫的版本號。
- _meta: 也是 Elasticsearch 7.8 新增的功能，讓使用者自己存放任意的物件資料。

- number_of_shards是指索引要做多少個分片，只能在創建索引時指定，後期無法修改。
- number_of_replicas是指每個分片有多少個副本，後期可以動態修改

----
**一個index要分配幾個shards?**
Shards 的數量跟index有關，依照每一個不同的index使用頻率和大小可以決定不同的primary shard數量，假如一個index大小超過100GB，那可能要切到4~5個shards，而不是你有幾個node決定，官方的影片也有提到假如在一個節點上，從一個shard提升到兩個shards效能有比較明顯的提升，再多反而會效能下降，建議是一個node上一個primary shard 和 replica shard就可以了。


[Elasticsearch 基本原理及規劃](https://jeff-yen.medium.com/elasticsearch-%E5%9F%BA%E6%9C%AC%E5%8E%9F%E7%90%86%E5%8F%8A%E8%A6%8F%E5%8A%83-e1763b856a08)

[Shared 概念複習](https://ithelp.ithome.com.tw/articles/10236906)

[C# Nest 程式作法](https://discuss.elastic.co/t/create-index-pattern-from-c-nest/249113)


##### Component Template

- 建立可被 Index Template 重覆使用的一組設定樣版，而可以設定的內容如下：
  - template: 可以包含 Aliases, Mappings, Index Settings 的設定。
  - version: 讓使用者自己編寫的版本號。
  - _meta: 讓使用者自己存放任意的物件資料。


[資料來源](https://ithelp.ithome.com.tw/articles/10239736)

#### Index alias

用途：為index建立一個特定的filter，稱作alias(別名)，以後就能透過alias做快速的條件查詢。

[資料來源](https://ithelp.ithome.com.tw/articles/10241035)

#### Object Fields VS Nested Field

##### 1. Object field [Link](https://www.elastic.co/guide/en/elasticsearch/reference/current/object.html)

```bash
PUT my-index-000001/_doc/1
{ 
  "region": "US",
  "manager": { 
    "age":     30,
    "name": { 
      "first": "John",
      "last":  "Smith"
    }
  }
}
```
實際上：
```bash
{
  "region":             "US",
  "manager.age":        30,
  "manager.name.first": "John",
  "manager.name.last":  "Smith"
}
```



##### 2. Nested field [Link](https://www.elastic.co/guide/en/elasticsearch/reference/current/nested.html)

```bash
PUT my-index-000001/_doc/1
{
  "group" : "fans",
  "user" : [ 
    {
      "first" : "John",
      "last" :  "Smith"
    },
    {
      "first" : "Alice",
      "last" :  "White"
    }
  ]
}
```
實際上：
```bash
{
  "group" :        "fans",
  "user.first" : [ "alice", "john" ],
  "user.last" :  [ "smith", "white" ]
}
```
這樣會造成失去了John與Smith 或 Alice與White之間的關聯
因此欄位型別應該要一開始就設定為nested：
```bash
PUT my-index-000001
{
  "mappings": {
    "properties": {
      "user": {
        "type": "nested" 
      }
    }
  }
}
```



[資料來源](https://opster.com/guides/elasticsearch/data-structuring/elasticsearch-nested-field-object-field/)

### ES優化
#### Indexing 索引效能優化

[資料來源](https://ithelp.ithome.com.tw/articles/10252325)



#### Shared 優化
[資料來源](https://ithelp.ithome.com.tw/articles/10253348)



### NEST套件

Create Index 

- [Attribute Map](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/attribute-mapping.html)
 
- [AutoMap](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/auto-map.html)