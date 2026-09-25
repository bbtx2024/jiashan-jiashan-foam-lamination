namespace QA.Business.Define
{
    public enum EN_PickOpportunity
    {
        /// <summary>
        /// MP（量产模式）使用。
        /// 上视觉ok数与启用吸嘴数一致或拍完所有穴位，再贴合。
        /// 提前取tape，取料数等于启用吸嘴数。
        /// 取tape时，如果启用吸嘴为偶数个，需要确保平台上无物料，防止物料粘在平台上。
        /// </summary>
        AfterStart,
        /// <summary>
        /// NPI（打样模式）使用。
        /// 上视觉每当拍照数达到启用吸嘴数或拍完所有穴位，就要检查是否有ok，有则贴合无则重复上视觉步骤。
        /// 上视觉结束后再取tape，取料数必须与上视觉ok数一致。
        /// 取tape时不管平台上是否还有物料，不考虑物料粘在平台上的问题。
        /// </summary>
        AfterUpCam,
    }
}
