using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{

    [Category("Animator")]
    [Description("设置 Animator 的 Bool 参数")]
    public class SetAnimatorBool : ActionTask<Animator>
    {

        [RequiredField]
        public BBParameter<string> parameterName;
        public BBParameter<bool> value = true;

        protected override string info => $"Set Bool [{parameterName}] = {value}";

        protected override void OnExecute()
        {
            if (agent != null)
            {
                agent.SetBool(parameterName.value, value.value);
            }
            EndAction(true);
        }
    }
}
