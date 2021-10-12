## UnityCombineMesh

支持多维子材质的动态合并脚本
![image](image/cms1.png)

## 使用说明

请将需要动态合并的MeshRender(不支持Skin)放入CombineMesh子节点下，点击Calc Vertex Count确保合并后的顶点数不超过65536，然后点击CombineRuntime后合并。

1. 模型需要打开Read/Write权限。
2. Excluded是排除合并列表。
   