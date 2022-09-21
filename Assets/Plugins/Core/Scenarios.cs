using Core.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Scenarios
{
    public interface IScenario
    {
        string ScenarioName { get; }
        void Start(IScenario previoslyScenario);
        void Pause();
        void Stop();
        bool IsActive { get; }
        bool Update();
        string Next();
        void SetMemory(Memory mem);
    }

    public class ScenarioTree : ISupportCoroutines
    {
        public const string InitKey = "init";

        private Memory _localMemory;
        Dictionary<string, IScenario> tree;
        private CoroutineExecuter coroutineExe = new CoroutineExecuter();
        private string _activeSceneName = string.Empty;
        /// <summary>
        /// Название активного сценария
        /// </summary>
        public string ActiveSceneName => _activeSceneName;
        /// <summary>
        /// Возвращает сценарий по его названию
        /// </summary>
        /// <param name="scenarioName"></param>
        /// <returns></returns>
        public IScenario GetScenario(string scenarioName) => tree[scenarioName];
        /// <summary>
        /// Создает новое древо сценариев с фиксированными сценариями
        /// </summary>
        /// <param name="scenarios">Может быть например<list type="bullet">
        /// <item><see cref="IEnumerable(IScenario)"/></item>
        /// <item><see cref="List(IScenario)"/></item>
        /// <item><see cref="IScenario"/>[]</item>
        /// </list></param>
        public ScenarioTree(IEnumerable<IScenario> scenarios)
        {
            var list = new List<IScenario>(scenarios);
            tree = new Dictionary<string, IScenario>(list.Count);
            _localMemory = new Memory(list.Count * 4, list.Count * 4);
           
            foreach (var sc in list)
            {
                tree.Add(sc.ScenarioName, sc);
                sc.SetMemory(_localMemory);
            }
        }
        /// <summary>
        /// Запущено ли древо сценариев?
        /// </summary>
        public bool IsRunned => isRunned;
        private bool isRunned = false;
        /// <summary>
        /// Запускает древо сценариев с <see cref="ScenarioTree.InitKey"/>
        /// </summary>
        /// <returns>Сопрограмма</returns>
        public IEnumerator RunScene() => RunScene(InitKey);
        /// <summary>
        /// Запускает древо сценариев с определенного сценария
        /// </summary>
        /// <param name="sceneName">Название сценария</param>
        /// <returns>Сопрограмма</returns>
        public IEnumerator RunScene(string sceneName)
        {
            if (isRunned) throw new System.InvalidProgramException("Tree is runned");
            isRunned = true;
            IScenario scenario;
            IScenario prevScenario = null;
            _localMemory.Flush();
            _localMemory.Write("root", this);
            coroutineExe.StopAllCoroutines();

        LOOP:

            if (tree.TryGetValue(sceneName, out scenario))
            {
                Debug.Log($"Swithed to scenario: {sceneName}");
                TryStart(); //scenario.Start(prevScenario);
                _activeSceneName = sceneName;
                while (!TryUpdate()/*scenario.Update()*/)
                {
                    coroutineExe.Update(Time.deltaTime);//coroutines
                    yield return null;
                }
                TryStop(); //scenario.Stop();
                prevScenario = scenario;
                var nextSceneName = TryNext(); //scenario.Next();
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    sceneName = nextSceneName;
                    goto LOOP;
                }
                else goto END;
            }
            else goto END;

            #region try
            void TryStart()
            {
                try
                {
                    scenario.Start(prevScenario);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{scenario.ScenarioName}.Start throwed");
                    Debug.LogException(e);
                    OnScenarioExceptionThrowHandler?.Invoke(scenario, ScenarioExceptionMethod.Start, e);
                }
            }
            bool TryUpdate()
            {
                try
                {
                    return scenario.Update();
                }
                catch (Exception e)
                {

                    Debug.LogError($"{scenario.ScenarioName}.Update throwed");
                    Debug.LogException(e);
                    OnScenarioExceptionThrowHandler?.Invoke(scenario, ScenarioExceptionMethod.Update, e);
                    return false;
                }
            }
            void TryStop()
            {
                try
                {
                    scenario.Stop();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{scenario.ScenarioName}.Stop throwed");
                    Debug.LogException(e);
                    OnScenarioExceptionThrowHandler?.Invoke(scenario, ScenarioExceptionMethod.Stop, e);
                }
            }
            string TryNext()
            {
                try
                {
                    return scenario.Next();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{scenario.ScenarioName}.Next throwed");
                    Debug.LogException(e);
                    OnScenarioExceptionThrowHandler?.Invoke(scenario, ScenarioExceptionMethod.Next, e);
                    return TryNext2();
                }

                string TryNext2()
                {
                    try
                    {
                        return scenario.Next();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"{scenario.ScenarioName}.Next_2 throwed");
                        Debug.LogException(e);
                        OnScenarioExceptionThrowHandler?.Invoke(scenario, ScenarioExceptionMethod.Next, e);
                        return null;
                    }
                }
            }
        #endregion

        END:
            Debug.Log($"Scenario tree end");
            isRunned = false;
            _activeSceneName = string.Empty;
            yield break;
        }

        public int StartCoroutine(IEnumerator<float> routine)
        {
            return ((ISupportCoroutines)coroutineExe).StartCoroutine(routine);
        }

        public bool StopCoroutine(int reference)
        {
            return ((ISupportCoroutines)coroutineExe).StopCoroutine(reference);
        }

        public void StopAllCoroutines()
        {
            ((ISupportCoroutines)coroutineExe).StopAllCoroutines();
        }

        public bool IsActiveCoroutine(int reference)
        {
            return ((ISupportCoroutines)coroutineExe).IsActiveCoroutine(reference);
        }

        public enum ScenarioExceptionMethod : byte { Unknow, Start, Update, Stop, Next }
        public delegate void ScenarioException(IScenario throwedScenario, ScenarioExceptionMethod throwedMethod, Exception throwedException);
        public event ScenarioException OnScenarioExceptionThrowHandler;
    }

    public abstract class Scenario : IScenario
    {
        /// <summary>
        /// Название сценария, должно быть константой времени выполнения
        /// </summary>
        public abstract string ScenarioName { get; }
        /// <summary>
        /// Вызывается при инициализации сценария
        /// </summary>
        /// <param name="previoslyScenrio"></param>
        protected abstract void Start(IScenario previoslyScenrio);
        /// <summary>
        /// Вызывается при завершении сценария
        /// </summary>
        protected abstract void Stop();
        /// <summary>
        /// Название следующего сценария. Вызывается после метода Stop()
        /// </summary>
        /// <returns></returns>
        protected abstract string NextScene();
        /// <summary>
        /// Обновляется каждый кадр, пока сценарий активен. Как только метод вернет значение true - сценарий считается выполненым
        /// </summary>
        /// <returns></returns>
        protected abstract bool Update();
        /// <summary>
        /// Запущен ли сценарий?
        /// </summary>
        private bool isRunned = false;
        /// <summary>
        /// Находится ли сценарий на паузе?
        /// </summary>
        private bool isPaused = false;
        private Memory _localMemory;
        /// <summary>
        /// Локальная память сценариев, ограниченная древом сценариев <see cref="ScenarioTree"/>
        /// </summary>
        protected Memory LocalMemory => _localMemory;
        public bool IsActive => isRunned && !isPaused;
        string IScenario.Next() => NextScene();
        bool IScenario.Update() => Update();
        void IScenario.Start(IScenario prevScenario)
        {
            if (!isRunned)
            {
                isRunned = true;
                isPaused = false;
                Start(prevScenario);
            }
        }
        void IScenario.SetMemory(Memory mem) => _localMemory = mem;
        void IScenario.Pause() => isPaused = !isPaused;
        void IScenario.Stop()
        {
            if (isRunned)
            {
                isRunned = false;
                Stop();
            }
        }

        protected ScenarioTree Root
        {
            get
            {
                if(_root == null)
                {
                    _localMemory.Read("root", out _root);
                }
                return _root;
            }
        }
        private ScenarioTree _root;

        protected int StartCoroutine(IEnumerator<float> routine)
        {
            return ((ISupportCoroutines)Root).StartCoroutine(routine);
        }

        protected bool StopCoroutine(int reference)
        {
            return ((ISupportCoroutines)Root).StopCoroutine(reference);
        }

        protected void StopAllCoroutines()
        {
            ((ISupportCoroutines)Root).StopAllCoroutines();
        }

        protected bool IsActiveCoroutine(int reference)
        {
            return ((ISupportCoroutines)Root).IsActiveCoroutine(reference);
        }
    }
}
