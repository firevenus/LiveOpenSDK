## 功能：
* 自动显示所有触摸Touch、鼠标按钮MouseButton的状态、动态位置跟随、是否Began OverUI、鼠标滚轮信息
* 自动日志，有变更的信息才会打印，日志可开关、且开关可自定义为外部可编程函数

## 接入使用方法：
1. MultiTouchDebugger目录放入工程的`Assets`内的任意目录
2. 目录中的 `MultiTouchDebugger.prefab` 拖动到场景里
3. Inspector属性，设置下空缺的 Target Rect Transform
4. 注：调试触摸位置生成的形状会生成在 Target Rect Transform 里面，建议将其设置引用为全屏的UI Canvas

* note: 这个小工具的设计形式，基础版本参考于、致谢给: Waldo, Press Start, 2018, https://pressstart.vip/tutorials/2018/11/14/81/multiple-touch-inputs.html
