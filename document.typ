#set text(font: "New Computer Modern") 
#align(center, text(16pt)[
  *互動程式設計三 期中專案*
])
#align(right, [資工四 110590004 林奕廷])

= 遊戲簡述
此遊戲為一款自走西洋棋遊戲，兩名玩家扮演不能動的國王角色，每回合輪流扮演攻擊者與防禦者角色，玩家每回合會抽取數個棋子並放置，並且每回合每個棋子(含玩家)會各自獲得數個增益(攻擊力加成、免疫、回血、防禦加成)。當兩方玩家都放置完棋子後，棋子會開始自動依照特性進行移動與攻擊。當玩家所代表的國王棋血量歸零及代表該玩家落敗。每局十回合，三戰兩勝打到死制。

= 繳交內容
Flowchart 及 Class Diagram 如後兩頁。若想仔細看，可以查看附檔中完整圖。
程式碼如附檔。若需檢查完整專案可至 #link("https://github.com/Error0229/LCRPG","GitHub Repo") 訪問。

= Flow Chart
#image("FSM.svg", height: 90%)

= Class Diagram
#image("Assets/puml/ClassDiagram.svg")
