/*
 * 金庸群侠传3D重制版
 * https://github.com/jynew/jynew
 *
 * 这是本开源项目文件头，所有代码均使用MIT协议。
 * 但游戏内资源和第三方插件、dll等请仔细阅读LICENSE相关授权协议文档。
 *
 * 金庸老先生千古！
 */
using Jyx2;
using Jyx2.Middleware;
using HSFrameWork.ConfigTable;
using Jyx2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Jyx2.Battle;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleActionUIPanel : Jyx2_UIBase
{
    public RoleInstance GetCurrentRole()
    {
        return m_currentRole;
    }
    
    RoleInstance m_currentRole;
    //BattleManager.BattleViewStates m_currentState;
    SkillUIItem m_selectItem;
    bool m_chooseBtn = false;
    List<SkillUIItem> m_curItemList = new List<SkillUIItem>();
    ChildGoComponent childMgr;

    private bool isSelectMove;
    private Action<BattleLoop.ManualResult> callback;
    private List<BattleBlockVector> moveRange;
    private BattleFieldModel battleModel;
    private BattleZhaoshiInstance currentZhaoshi;

    protected override void OnCreate()
    {
        InitTrans();
        childMgr = GameUtil.GetOrAddComponent<ChildGoComponent>(Skills_RectTransform);
        childMgr.Init(SkillItem_RectTransform);

        BindListener(Move_Button, OnMoveClick);
        BindListener(UsePoison_Button, OnUsePoisonClick);
        BindListener(Depoison_Button, OnDepoisonClick);
        BindListener(Heal_Button, OnHealClick);
        BindListener(Item_Button, OnUseItemClick);
        BindListener(Wait_Button, OnWaitClick);
        BindListener(Rest_Button, OnRestClick);
        BindListener(Cancel_Button, OnCancelClick);
    }

    //Jyx2_UIManager.Instance.ShowUI(nameof(BattleActionUIPanel),role, moveRange, moved, callback);
    
    
    
    protected override void OnShowPanel(params object[] allParams)
    {
        base.OnShowPanel(allParams);
        m_currentRole = allParams[0] as RoleInstance;
        if (m_currentRole == null)
            return;
        /*if (allParams.Length > 1)
            m_currentState = (BattleManager.BattleViewStates) allParams[1];*/

        moveRange = (List<BattleBlockVector>) allParams[1];
        isSelectMove = (bool) allParams[2];
        callback = (Action<BattleLoop.ManualResult>) allParams[3];
        battleModel = BattleManager.Instance.GetModel();

        //Cancel_Button.gameObject.SetActive(false);
        SetActionBtnState();
        RefreshSkill();
        //SetPanelState();
        
        if (isSelectMove)
        {
            BattleboxHelper.Instance.ShowBlocks(moveRange);
        }
        else
        {
            var zhaoshi = m_curItemList[0].GetSkill();
            ShowAttackRangeSelector(zhaoshi);
        }
    }

    //显示攻击范围选择指示器
    void ShowAttackRangeSelector(BattleZhaoshiInstance zhaoshi)
    {
        currentZhaoshi = zhaoshi;

        isSelectMove = false;
        BattleboxHelper.Instance.HideAllBlocks();
        var blockList = BattleManager.Instance.GetSkillUseRange(m_currentRole, zhaoshi);
        BattleboxHelper.Instance.ShowBlocks(blockList, BattleBlockType.AttackZone);
    }

    void Update()
    {
        //寻找玩家点击的格子
        var block = InputManager.Instance.GetMouseUpBattleBlock();
        
        //没有选择格子
        if (block == null) return;
        
        //格子隐藏（原则上应该不会出现）
        if (block.gameObject.activeSelf == false) return;
        
        //选择移动，但位置站人了
        if (isSelectMove && battleModel.BlockHasRole(block.BattlePos.X, block.BattlePos.Y)) return;

        //隐藏格子
        BattleboxHelper.Instance.HideAllBlocks();
        
        //以下进行回调
        
        //移动
        if (isSelectMove)
        {
            TryCallback(new BattleLoop.ManualResult() {movePos = block.BattlePos}); //移动
        }
        else  //选择攻击
        {
            AIResult rst = new AIResult();
            rst.AttackX = block.BattlePos.X;
            rst.AttackY = block.BattlePos.Y;
            
            rst.Zhaoshi = currentZhaoshi;

            TryCallback(new BattleLoop.ManualResult() {aiResult = rst});
        }
    }

    void TryCallback(BattleLoop.ManualResult ret)
    {
        callback?.Invoke(ret);
    }

    //点击了自动
    public void OnAutoClicked()
    {
        TryCallback(new BattleLoop.ManualResult() {isAuto = true});
    }

    protected override void OnHidePanel()
    {
        base.OnHidePanel();
        //m_currentState = BattleManager.BattleViewStates.None;
        m_currentRole = null;
        m_selectItem = null;
        m_curItemList.Clear();
        
        //隐藏格子
        BattleboxHelper.Instance.HideAllBlocks();
    }

    void SetActionBtnState()
    {
        bool canPoison = m_currentRole.UsePoison > 0 && m_currentRole.Tili >= 30;
        UsePoison_Button.gameObject.SetActive(canPoison);
        bool canDepoison = m_currentRole.DePoison > 0 && m_currentRole.Tili >= 30;
        Depoison_Button.gameObject.SetActive(canDepoison);
        bool canHeal = m_currentRole.Heal > 0 && m_currentRole.Tili >= 10;
        Heal_Button.gameObject.SetActive(canHeal);

        bool lastRole = BattleManager.Instance.GetModel().IsLastRole(m_currentRole);
        Wait_Button.gameObject.SetActive(!lastRole);

        /*Cancel_Button.gameObject.SetActive(m_currentState == BattleManager.BattleViewStates.SelectMove
                                           || m_currentState == BattleManager.BattleViewStates.SelectSkill);*/
    }

    void RefreshSkill()
    {
        m_curItemList.Clear();
        var zhaoshis = m_currentRole.GetZhaoshis(true).ToList();
        childMgr.RefreshChildCount(zhaoshis.Count);
        List<Transform> childTransList = childMgr.GetUsingTransList();
        for (int i = 0; i < zhaoshis.Count; i++)
        {
            SkillUIItem item = GameUtil.GetOrAddComponent<SkillUIItem>(childTransList[i]);
            item.RefreshSkill(zhaoshis[i]);
            item.SetSelect(m_selectItem == item);

            Button btn = item.GetComponent<Button>();
            BindListener(btn, () => { OnItemClick(item); });
            m_curItemList.Add(item);
        }
    }

    void OnItemClick(SkillUIItem item)
    {
        if (m_selectItem == item)
            return;
        if (m_selectItem != null)
            m_selectItem.SetSelect(false);
        m_selectItem = item;
        m_chooseBtn = false;

        m_currentRole.SwitchAnimationToSkill(m_selectItem.GetSkill().Data);
        ShowAttackRangeSelector(m_selectItem.GetSkill());
    }

    void OnCancelClick()
    {
        TryCallback(new BattleLoop.ManualResult() {isRevert = true});
    }

    void OnMoveClick()
    {

    }

    void OnUsePoisonClick()
    {
        var zhaoshi = new PoisonZhaoshiInstance(m_currentRole.UsePoison);
        m_chooseBtn = true;
        ShowAttackRangeSelector(zhaoshi);
    }

    void OnDepoisonClick()
    {
        var zhaoshi = new DePoisonZhaoshiInstance(m_currentRole.DePoison);
        m_chooseBtn = true;
        ShowAttackRangeSelector(zhaoshi);
    }

    void OnHealClick()
    {
        var zhaoshi = new HealZhaoshiInstance(m_currentRole.Heal);
        m_chooseBtn = true;
        ShowAttackRangeSelector(zhaoshi);
    }

    void OnUseItemClick()
    {
        bool Filter(Jyx2Item item) => item.ItemType == 3 || item.ItemType == 4;

        Jyx2_UIManager.Instance.ShowUI(nameof(BagUIPanel), GameRuntimeData.Instance.Items, new Action<int>((itemId) =>
        {

            if (itemId == -1)
                return;

            var item = ConfigTable.Get<Jyx2Item>(itemId);
            if (item.ItemType == 3) //使用道具逻辑
            {
                if (m_currentRole.CanUseItem(itemId))
                {
                    TryCallback(new BattleLoop.ManualResult(){aiResult = new AIResult(){Item = item}});
                }
            }
            else if (item.ItemType == 4) //使用暗器逻辑
            {
                var zhaoshi = new AnqiZhaoshiInstance(m_currentRole.Anqi, item);
                m_chooseBtn = true;
                ShowAttackRangeSelector(zhaoshi);
            }

        }), (Func<Jyx2Item, bool>) Filter);
    }

    void OnWaitClick()
    {
        TryCallback(new BattleLoop.ManualResult() {isWait = true});
    }

    void OnRestClick()
    {
        TryCallback(new BattleLoop.ManualResult() {aiResult = new AIResult() {IsRest = true}});
    }
}
